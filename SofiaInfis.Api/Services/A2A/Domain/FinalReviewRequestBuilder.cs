using System.Text;
using System.Text.Json;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Services.A2A.Transport;

namespace SofiaInfis.Api.Services.A2A.Domain;

public static class FinalReviewRequestBuilder
{
    public const string Stage = "parecer_final";
    public const string DocumentId = "sofia-final-review";
    public static List<DocumentoEntrada> Build(CriarOrquestracaoRequest original, string executionId,
        IReadOnlyList<AgentCallDocument> calls)
    {
        var payload = JsonSerializer.Serialize(new
        {
            versao = "parecer-final-sofia.v1", idExecucao = executionId,
            perguntaOriginal = original.pergunta,
            especialistas = calls.Where(x => x.Etapa != Stage).OrderBy(x => x.Ordem).Select(call => new
            {
                idAgente = call.IdAgente, idSolicitacao = call.Id, estado = call.Estado,
                resultado = call.ResultadoJson is null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(call.ResultadoJson),
                falha = call.FalhaTecnica
            }),
            limitacoes = "Falhas e resultados parciais não comprovam conformidade. Não presumir validação de origem, dados ausentes ou conclusão sem evidências."
        }, new JsonSerializerOptions(JsonWire.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never });
        // Preserve the existing per-document limit; never silently trim an agent's result.
        if (payload.EnumerateRunes().Count() > 20000)
            throw new A2AException("validacao", "FINAL_REVIEW_INPUT_TOO_LARGE", 413);
        return [new(DocumentId, "parecer-final-sofia.v1.json", payload, "application/json")];
    }
}
