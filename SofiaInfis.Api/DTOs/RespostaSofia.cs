namespace SofiaInfis.Api.DTOs
{
    public record RespostaSofia(
        Guid idExecucao,
        string pergunta,
        string resposta,
        List<string> arquivosProcessados,
        List<ConhecimentoResultado> conhecimentosUtilizados,
        int tokensEntrada,
        int tokensSaida,
        int tokensTotal,
        long duracaoMs
    );
}
