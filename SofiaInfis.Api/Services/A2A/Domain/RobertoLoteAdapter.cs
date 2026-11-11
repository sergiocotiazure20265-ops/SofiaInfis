using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Json.Schema;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Transport;

namespace SofiaInfis.Api.Services.A2A.Domain;

public sealed record LoteAdaptation(List<DocumentoEntrada>? Documents, string[] Limitations)
{
    public bool Ready => Documents is not null;
}

/// <summary>Deterministic contract adapter. Origin approval is supplied evidence, never inferred from prose.</summary>
public static class RobertoLoteAdapter
{
    private static readonly Lazy<JsonSchema> Schema = new(() =>
    {
        using var stream = typeof(RobertoLoteAdapter).Assembly.GetManifestResourceStream(
            "SofiaInfis.Api.Services.A2A.Contracts.roberto-lote.v1.schema.json")!;
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    });
    private static readonly Lazy<JsonSchema> ObligationSchema = new(() =>
    {
        using var stream = typeof(RobertoLoteAdapter).Assembly.GetManifestResourceStream("SofiaInfis.Api.Services.A2A.Contracts.roberto-lote.v1.schema.json")!;
        using var reader = new StreamReader(stream);
        var root = JsonNode.Parse(reader.ReadToEnd())!;
        return JsonSchema.FromText(new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$defs"] = root["$defs"]!.DeepClone(), ["$ref"] = "#/$defs/obrigacao"
        }.ToJsonString());
    });

    public static LoteAdaptation Adapt(IReadOnlyList<DocumentoEntrada> documents, string executionId)
    {
        var issues = new List<string>();
        var obligations = new JsonArray();
        var parsed = new Dictionary<string, JsonObject>();
        foreach (var doc in documents.Where(x => x.idDocumento != AgentRequestRouter.QuestionContextId))
        {
            if (!doc.texto.TrimStart().StartsWith('{')) continue;
            try
            {
                using var json = JsonWire.Parse(Encoding.UTF8.GetBytes(doc.texto));
                if (JsonNode.Parse(json.RootElement.GetRawText()) is JsonObject obj) parsed.Add(doc.idDocumento, obj);
            }
            catch (A2AException) { issues.Add($"{doc.nome}: JSON inválido ou com propriedades duplicadas."); }
        }
        var reports = parsed.Values.Where(x => x["documento"] is JsonValue).ToArray();
        foreach (var doc in documents.Where(x => x.idDocumento != AgentRequestRouter.QuestionContextId))
        {
            parsed.TryGetValue(doc.idDocumento, out var obj);
            if (obj?["documento"] is not null) continue;
            if (obj?["obrigacoes"] is JsonArray items)
            {
                if (!Schema.Value.Evaluate(obj).IsValid) { issues.Add($"{doc.nome}: lote não atende ao schema lote.v1."); continue; }
                if (!ValidRoot(Text(obj["contribuinte"], "cnpj")) || !ValidPeriod(obj["periodo"]))
                    issues.Add($"{doc.nome}: contribuinte ou período do lote inválido.");
                foreach (var item in items)
                {
                    obligations.Add(item?.DeepClone());
                    if (item is not null && (Text(item["contribuinte"], "cnpj")[..Math.Min(8, Text(item["contribuinte"], "cnpj").Length)] != Text(obj["contribuinte"], "cnpj")[..Math.Min(8, Text(obj["contribuinte"], "cnpj").Length)] || !Overlaps(item["periodo"], obj["periodo"])))
                        issues.Add($"{doc.nome}: obrigação fora do contribuinte/período declarado no lote.");
                }
            }
            else if (obj?["tipo"] is JsonValue && obj["registros"] is JsonObject) obligations.Add(obj.DeepClone());
            else
            {
                var matching = reports.Where(x => Text(x, "documento") == doc.nome).ToArray();
                if (matching.Length != 1 || documents.Count(x => x.nome == doc.nome) != 1)
                {
                    issues.Add($"{doc.nome}: falta relatório único de validação de origem associado ao arquivo, com versão, data e status explícitos; análises em texto não substituem esse relatório.");
                    continue;
                }
                var normalized = FiscalDocumentNormalizer.Normalize(doc, matching[0], issues);
                if (normalized is not null) obligations.Add(normalized);
            }
        }
        foreach (var report in reports)
            if (documents.Count(x => x.nome == Text(report, "documento")) != 1)
                issues.Add("Relatório de validação sem um único arquivo de origem correspondente.");
        if (obligations.Count == 0) issues.Add("Não há obrigações fiscais normalizadas suficientes para montar o lote.");
        if (obligations.Count > 500) issues.Add("O lote excede 500 obrigações.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in obligations.OfType<JsonObject>())
        {
            if (!ids.Add(Text(item, "id"))) issues.Add("Há identificadores de obrigação duplicados.");
            ValidateObligation(item, issues);
        }
        var roots = obligations.OfType<JsonObject>().Select(x => Text(x["contribuinte"], "cnpj")).Where(x => x.Length >= 8).Select(x => x[..8]).Distinct().ToArray();
        if (roots.Length != 1) issues.Add("Não foi identificado um único contribuinte para o lote; separe empresas de raízes diferentes.");
        if (issues.Count > 0) return new(null, issues.Distinct().ToArray());
        var periods = obligations.Select(x => x!["periodo"]!).ToArray();
        var lote = new JsonObject
        {
            ["versaoContrato"] = "1.0.0", ["loteId"] = executionId,
            ["contribuinte"] = new JsonObject { ["cnpj"] = roots[0] },
            ["periodo"] = new JsonObject { ["inicio"] = periods.Min(x => Text(x, "inicio")), ["fim"] = periods.Max(x => Text(x, "fim")) },
            ["obrigacoes"] = obligations
        };
        if (!Schema.Value.Evaluate(lote).IsValid) return new(null, ["Há campos obrigatórios ausentes ou incompatíveis com o schema lote.v1."]);
        var text = lote.ToJsonString(JsonWire.Options);
        if (text.EnumerateRunes().Count() > 20000) return new(null, ["O lote excede o limite de 20.000 caracteres por documento; nenhum registro foi truncado."]);
        return new([new(FinalReviewRequestBuilder.DocumentId, "lote.v1.json", text, "application/json")],
            ["A validação de origem foi preservada conforme os relatórios fornecidos, sem certificação adicional da SOFIA. O agente do Roberto executa os cruzamentos que suporta; tipos sem regras implementadas permanecem sem conclusão fiscal."]);
    }

    private static void ValidateObligation(JsonObject item, List<string> issues)
    {
        var id = Text(item, "id");
        void Missing(string message) => issues.Add($"Obrigação {id}: {message}");
        if (!ObligationSchema.Value.Evaluate(item).IsValid) { Missing("estrutura incompleta; são necessários contribuinte, período, identificação, registros e relatório de validação de origem no contrato oficial."); return; }
        if (!ValidCnpj(Text(item["contribuinte"], "cnpj"))) Missing("CNPJ ausente ou dígitos verificadores inválidos.");
        if (!ValidPeriod(item["periodo"])) Missing("período ausente ou inválido.");
        var type = Text(item, "tipo");
        var layout = Text(item, "versaoLayout");
        var expected = type switch { "NFE" => "4.00", "NFSE" => "1.01", "EFD_ICMS_IPI" => "020", "EFD_CONTRIBUICOES" => "006", "ECD" => "9.00", "ECF" => "012", "DIMP" => Text(item["periodo"], "inicio").CompareTo("2026-06-01") < 0 ? "09" : "10", _ => "" };
        if (expected.Length == 0 || layout != expected) Missing("tipo/layout fora do escopo publicado pelo agente do Roberto.");
        if (item["registros"] is not JsonObject records || !records.Any(group => group.Value is JsonArray entries && entries.Any(entry => entry?["campos"] is JsonObject fields && fields.Any(field => field.Value is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text)))))
            Missing("registros fiscais ausentes.");
        if (type is "NFE" or "NFSE")
        {
            var key = Text(item["identificacao"], "chaveAcesso");
            if (key.Length != 44 || key.Any(c => !char.IsAsciiDigit(c) && !char.IsAsciiLetterUpper(c))) Missing("chave de acesso de 44 posições ausente ou inválida.");
        }
        else if (string.IsNullOrWhiteSpace(Text(item["identificacao"], "recibo")) && string.IsNullOrWhiteSpace(Text(item["identificacao"], "hash"))) Missing("recibo/hash de origem ausente.");
        if (Text(item, "finalidade") == "SUBSTITUTA" && string.IsNullOrWhiteSpace(Text(item["identificacao"], "reciboAnterior"))) Missing("recibo da escrituração substituída ausente.");
        var validation = item["validacaoOrigem"];
        if (string.IsNullOrWhiteSpace(Text(validation, "agente")) || string.IsNullOrWhiteSpace(Text(validation, "versaoValidador")) ||
            !DateTimeOffset.TryParse(Text(validation, "dataValidacao"), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Missing("relatório de validação de origem incompleto.");
        if (Text(validation, "status") != "APROVADO" || validation?["ocorrencias"] is not JsonArray occurrences || occurrences.Any(x => Text(x, "tipo") == "ERRO"))
            Missing("validação de origem não comprova aprovação; não é seguro submeter ao cruzamento como obrigação apta.");
    }
    internal static string Text(JsonNode? node, string key) => node is JsonObject obj && obj[key] is JsonValue v && v.TryGetValue<string>(out var value) ? value : "";
    private static bool ValidPeriod(JsonNode? node) => DateOnly.TryParseExact(Text(node, "inicio"), "yyyy-MM-dd", out var start) && DateOnly.TryParseExact(Text(node, "fim"), "yyyy-MM-dd", out var end) && end >= start;
    private static bool Overlaps(JsonNode? a, JsonNode? b) => ValidPeriod(a) && ValidPeriod(b) && string.CompareOrdinal(Text(a, "inicio"), Text(b, "fim")) <= 0 && string.CompareOrdinal(Text(a, "fim"), Text(b, "inicio")) >= 0;
    private static bool ValidRoot(string cnpj) => cnpj.Length == 8 && cnpj.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)) || ValidCnpj(cnpj);
    internal static bool ValidCnpj(string value)
    {
        if (value.Length != 14 || value.Take(12).Any(c => !char.IsAsciiDigit(c) && !char.IsAsciiLetterUpper(c)) || !value.Skip(12).All(char.IsAsciiDigit) || value.All(c => c == value[0])) return false;
        static int Dv(string v, int[] weights) { var remainder = v.Select((c, i) => (c - 48) * weights[i]).Sum() % 11; return remainder < 2 ? 0 : 11 - remainder; }
        return value[12] - '0' == Dv(value[..12], [5,4,3,2,9,8,7,6,5,4,3,2]) && value[13] - '0' == Dv(value[..13], [6,5,4,3,2,9,8,7,6,5,4,3,2]);
    }
}
