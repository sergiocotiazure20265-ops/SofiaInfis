using System.Text.Json.Nodes;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Tests.Unit;

public sealed class RobertoLoteAdapterTests
{
    private static JsonObject Example() => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/Contracts/examples/roberto-lote.json")))!.AsObject();
    private static DocumentoEntrada Doc(JsonNode value, string id = "lote") => new(id, id + ".txt", value.ToJsonString());
    private static LoteAdaptation Adapt(params DocumentoEntrada[] documents) => RobertoLoteAdapter.Adapt(documents, Guid.NewGuid().ToString());

    [Fact]
    public void Official_lote_is_preserved_and_individual_obligations_are_assembled()
    {
        var example = Example();
        var adapted = Adapt(Doc(example));
        Assert.True(adapted.Ready, string.Join(";", adapted.Limitations));
        var output = JsonNode.Parse(Assert.Single(adapted.Documents!).texto)!;
        Assert.Equal("1.0.0", output["versaoContrato"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(example["obrigacoes"], output["obrigacoes"]));
        var standalone = Adapt(example["obrigacoes"]!.AsArray().Select((x, i) => Doc(x!, "obrigacao-" + i)).ToArray());
        Assert.True(standalone.Ready, string.Join(";", standalone.Limitations));
    }
    [Theory]
    [InlineData("missing-validation")]
    [InlineData("partial-validation")]
    [InlineData("error-with-approval")]
    [InlineData("bad-cnpj")]
    [InlineData("bad-date")]
    [InlineData("missing-key")]
    [InlineData("unsupported-layout")]
    [InlineData("duplicate-id")]
    [InlineData("mixed-company")]
    [InlineData("extra-property")]
    [InlineData("empty-records")]
    [InlineData("wrong-type")]
    public void Incomplete_or_unverified_input_never_becomes_an_approved_lote(string mode)
    {
        var example = Example(); var first = example["obrigacoes"]![0]!;
        switch (mode)
        {
            case "missing-validation": first.AsObject().Remove("validacaoOrigem"); break;
            case "partial-validation": first["validacaoOrigem"]!["status"] = "PARCIAL"; break;
            case "error-with-approval": first["validacaoOrigem"]!["ocorrencias"]!.AsArray().Add(new JsonObject { ["tipo"] = "ERRO", ["mensagem"] = "Falha real informada" }); break;
            case "bad-cnpj": first["contribuinte"]!["cnpj"] = "11222333000180"; break;
            case "bad-date": first["periodo"]!["fim"] = "2026-02-30"; break;
            case "missing-key": example["obrigacoes"]![1]!["identificacao"] = new JsonObject(); break;
            case "unsupported-layout": first["versaoLayout"] = "999"; break;
            case "duplicate-id": example["obrigacoes"]![1]!["id"] = first["id"]!.DeepClone(); break;
            case "mixed-company": example["obrigacoes"]![1]!["contribuinte"]!["cnpj"] = "11444777000161"; break;
            case "extra-property": first["aprovadoPelaSofia"] = true; break;
            case "empty-records": first["registros"] = new JsonObject(); break;
            case "wrong-type": first["validacaoOrigem"] = "APROVADO"; break;
        }
        var original = example.ToJsonString();
        var adapted = Adapt(Doc(example));
        Assert.False(adapted.Ready); Assert.NotEmpty(adapted.Limitations);
        Assert.Equal(original, example.ToJsonString());
    }
    [Theory]
    [InlineData("ECD sintética")]
    [InlineData("<Nfse/>")]
    [InlineData("Aprovado: documento correto, sem falhas.")]
    [InlineData("{\"obrigacoes\":[],\"obrigacoes\":[]}")]
    public void Prose_partial_results_and_ambiguous_json_do_not_supply_missing_facts(string text)
    {
        var result = Adapt(new DocumentoEntrada("doc", "documento.txt", text));
        Assert.False(result.Ready); Assert.NotEmpty(result.Limitations);
    }
    [Fact]
    public void An_unadaptable_document_is_not_silently_dropped_from_a_valid_lote()
    {
        var result = Adapt(Doc(Example()), new("extra", "ecd-extra.txt", "ECD sem registros nem relatório"));
        Assert.False(result.Ready); Assert.Contains(result.Limitations, x => x.Contains("ecd-extra.txt"));
    }
    [Fact]
    public void Large_lote_is_not_truncated()
    {
        var example = Example(); example["obrigacoes"]![0]!["registros"]!["0000"]![0]!["campos"]!["NOME"] = new string('x', 21000);
        var result = Adapt(Doc(example)); Assert.False(result.Ready);
        Assert.Contains(result.Limitations, x => x.Contains("20.000"));
    }

    private const string Key = "35260311222333000181550010000000011000000010";
    private const string Xml = "<nfeProc xmlns='http://www.portalfiscal.inf.br/nfe'><NFe><infNFe versao='4.00' Id='NFe" + Key + "'><ide><mod>55</mod><dhEmi>2026-03-10T09:00:00-03:00</dhEmi><tpNF>1</tpNF></ide><emit><CNPJ>11222333000181</CNPJ><enderEmit><UF>SP</UF></enderEmit></emit><dest><CNPJ>11444777000161</CNPJ></dest><total><ICMSTot><vNF>1000.00</vNF><vICMS>180.00</vICMS></ICMSTot></total></infNFe></NFe></nfeProc>";
    private static JsonObject Report(string name) => new()
    {
        ["documento"] = name, ["contribuinte"] = new JsonObject { ["cnpj"] = "11222333000181" },
        ["validacaoOrigem"] = Example()["obrigacoes"]![0]!["validacaoOrigem"]!.DeepClone(),
        ["identificacao"] = new JsonObject { ["recibo"] = "RECIBO-SINTETICO-DECLARADO" }
    };
    [Fact]
    public void Xml_values_and_validation_report_are_mapped_without_inventing_approval()
    {
        var report = Report("nota.xml");
        var result = Adapt(new DocumentoEntrada("nota", "nota.xml", Xml), Doc(report, "relatorio"));
        Assert.True(result.Ready, string.Join(";", result.Limitations));
        var item = JsonNode.Parse(result.Documents![0].texto)!["obrigacoes"]![0]!;
        Assert.Equal(Key, item["identificacao"]!["chaveAcesso"]!.GetValue<string>());
        Assert.Equal("180.00", item["registros"]!["ICMSTot"]![0]!["campos"]!["vICMS"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(report["validacaoOrigem"], item["validacaoOrigem"]));
        Assert.False(Adapt(new DocumentoEntrada("nota", "nota.xml", Xml)).Ready);
        report["validacaoOrigem"]!["status"] = "REPROVADO";
        Assert.False(Adapt(new DocumentoEntrada("nota", "nota.xml", Xml), Doc(report, "relatorio")).Ready);
    }
    [Fact]
    public void Sped_dates_decimals_and_origin_receipt_are_mapped_with_reviewed_positions()
    {
        const string sped = "|0000|020|0|01032026|31032026|TESTE|11222333000181||SP|123|3550308|||||\n|C100|1|0|CLI|55|00|1|1|" + Key + "|10032026|10032026|1000,00|0|0|0|1000|0|0|0|0|1000|180|0|0|0|0|0|0|0|\n|9999|3|";
        var report = Report("efd.txt");
        var result = Adapt(new DocumentoEntrada("efd", "efd.txt", sped), Doc(report, "relatorio"));
        Assert.True(result.Ready, string.Join(";", result.Limitations));
        var item = JsonNode.Parse(result.Documents![0].texto)!["obrigacoes"]![0]!;
        Assert.Equal("2026-03-01", item["periodo"]!["inicio"]!.GetValue<string>());
        Assert.Equal("180", item["registros"]!["C100"]![0]!["campos"]!["VL_ICMS"]!.GetValue<string>());
        report["identificacao"] = new JsonObject();
        Assert.False(Adapt(new DocumentoEntrada("efd", "efd.txt", sped), Doc(report, "relatorio")).Ready);
    }
}
