using MongoDB.Bson.Serialization.Attributes;
namespace SofiaInfis.Api.Services.A2A.Persistence;

public sealed class ExecutionDocument
{
    [BsonId] public string Id { get; set; } = "";
    public string OwnerClientId { get; set; } = "";
    public string OwnerRunId { get; set; } = "";
    public long Version { get; set; }
    public string Estado { get; set; } = "em_andamento";
    public string[] CapacidadesSolicitadas { get; set; } = [];
    public string[] SemCobertura { get; set; } = [];
    public string[] DocumentoIds { get; set; } = [];
    public string[] AgentesSelecionados { get; set; } = [];
    public DateTime CriadoEm { get; set; }
    public DateTime? IniciadoEm { get; set; }
    public DateTime? EncerradoEm { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? FalhaRegistro { get; set; }
}
