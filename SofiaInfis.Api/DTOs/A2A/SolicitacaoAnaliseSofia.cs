namespace SofiaInfis.Api.DTOs.A2A;
public record SolicitacaoAnaliseSofia(string versaoContrato, Guid idExecucao, Guid idSolicitacao,
    UsuarioContexto usuario, string pergunta, List<DocumentoAnalise> documentos, ContextoAnalise? contexto = null);
