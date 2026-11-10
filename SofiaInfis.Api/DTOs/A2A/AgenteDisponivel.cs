namespace SofiaInfis.Api.DTOs.A2A;
public record AgenteDisponivel(string idAgente, string nome, string[] capacidades, bool elegivel, string? motivo);
