using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Transport;
namespace SofiaInfis.Api.Tests.Contract;

public sealed class SofiaDomainContractTests
{
    internal static string Contracts => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Contracts");
    internal static JsonSchema Schema(string file, string fragment)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Contracts, file)))!.AsObject();
        root["$ref"] = fragment;
        return JsonSchema.FromText(root.ToJsonString());
    }
    [Theory]
    [InlineData("solicitacao-sofia.json", "SolicitacaoAnaliseSofia")]
    [InlineData("resultado-sofia.json", "ResultadoAnaliseSofia")]
    [InlineData("resultado-erro.json", "ResultadoAnaliseSofia")]
    public void Canonical_fixture_validates_and_required_fields_cannot_disappear(string file, string definition)
    {
        var schema = Schema("sofia-v1.schema.json", "#/$defs/" + definition);
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(Contracts, "examples", file)))!.AsObject();
        Assert.True(schema.Evaluate(node).IsValid);
        foreach (var field in new[] { "versaoContrato", "idExecucao", "idSolicitacao" })
        {
            var broken = (JsonObject)node.DeepClone();
            broken.Remove(field);
            Assert.False(schema.Evaluate(broken).IsValid);
        }
    }
    [Fact]
    public void Unicode_input_is_counted_as_scalars_and_invalid_documents_are_not_silently_truncated()
    {
        var request = new CriarOrquestracaoRequest(new("Pessoa"), "Analise estes textos", [new("doc", "a.txt", "texto")], ["analisar_documentos"]);
        var validator = new OrchestrationInputValidator(new());
        validator.Validate(request with { usuario = new(string.Concat(Enumerable.Repeat("😀", 200))) });
        Assert.Throws<A2AException>(() => validator.Validate(request with { documentos = [new("doc", "a", new string('x', 20001))] }));
        Assert.Throws<A2AException>(() => validator.Validate(request with { documentos = [new("doc", "a", "texto", textoCompleto: false)] }));
        Assert.Throws<A2AException>(() => validator.Validate(request with { documentos = [request.documentos[0], request.documentos[0]] }));
    }
    [Fact]
    public void Duplicate_json_property_is_rejected_before_model_binding()
        => Assert.Throws<A2AException>(() => JsonWire.Parse(System.Text.Encoding.UTF8.GetBytes("{\"pergunta\":\"a\",\"Pergunta\":\"b\"}")));
}
