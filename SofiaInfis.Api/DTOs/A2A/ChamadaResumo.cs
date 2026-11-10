using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.DTOs.A2A;
public record ChamadaResumo(string idSolicitacao, string idAgente, string estado, string? statusResultado,
    bool resultadoDisponivel, string? linkResultado, A2AFailure? falhaTecnica, long? duracaoMs, ConsumoInformado? consumo);
