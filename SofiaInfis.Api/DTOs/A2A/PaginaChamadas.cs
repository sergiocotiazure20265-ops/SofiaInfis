namespace SofiaInfis.Api.DTOs.A2A;
public record PaginaChamadas(IReadOnlyList<ChamadaResumo> items, string? cursorProximo);
