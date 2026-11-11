using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Services.A2A.Persistence;
public sealed class AgentCallDocument
{
    [BsonId] public string Id { get; set; } = "";
    public string IdExecucao { get; set; } = "";
    public string IdAgente { get; set; } = "";
    public string? MotivoSelecao { get; set; }
    public int? DocumentosEnviados { get; set; }
    public int Ordem { get; set; }
    public string OwnerRunId { get; set; } = "";
    public long Version { get; set; }
    public string Estado { get; set; } = "aguardando";
    public string? TaskId { get; set; }
    public string? ContextId { get; set; }
    public string? MessageId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public DateTime? EncerradoEm { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ResultadoJson { get; set; }
    public A2AFailure? FalhaTecnica { get; set; }
    public ConsumoInformado? Consumo { get; set; }
    public long? DuracaoMs { get; set; }
}
