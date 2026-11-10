namespace SofiaInfis.Api.DTOs.A2A;
public record CriarOrquestracaoRequest(UsuarioContexto usuario, string pergunta,
    List<DocumentoEntrada> documentos, List<string> capacidades);
