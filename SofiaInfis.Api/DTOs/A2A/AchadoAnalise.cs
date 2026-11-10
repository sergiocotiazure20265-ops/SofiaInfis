namespace SofiaInfis.Api.DTOs.A2A;
public record AchadoAnalise(string idAchado, string descricao, List<string> idDocumentos,
    List<string> evidencias, string? severidade = null);
