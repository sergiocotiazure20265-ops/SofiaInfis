namespace SofiaInfis.Api.DTOs.A2A;
public record DocumentoAnalise(string idDocumento, string nome, string texto, string? tipo = null);
public record DocumentoEntrada(string idDocumento, string nome, string texto, string? tipo = null, bool textoCompleto = true);
public record UsuarioContexto(string nome);
public record ContextoAnalise(string? idioma = null, string? instrucoes = null);
