namespace SofiaInfis.Api.DTOs.A2A;
public record ExecucaoConsulta(string idExecucao, string estado, DateTime criadoEm, DateTime? encerradoEm,
    DateTime expiresAt, string[] capacidadesSolicitadas, string[] capacidadesSemCobertura,
    PaginaChamadas chamadas, bool registroConfirmado, string? falhaRegistro = null);
