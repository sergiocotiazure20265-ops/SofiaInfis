using System.Text.Json;
using System.Text.Json.Serialization;

namespace SofiaInfis.Api.DTOs.A2A;
public record ResultadoAnaliseSofia(string versaoContrato, Guid idExecucao, Guid idSolicitacao,
    AgenteAnalise agente, string status, string analise, List<AchadoAnalise> achados,
    List<string>? observacoes = null, ErroAnalise? erro = null)
{
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
