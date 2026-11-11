using System.Text;
namespace SofiaInfis.Web.Services;

public static class FinalReviewAnswerComposer
{
    public static bool TryApply(ChatRun run)
    {
        var agent = run.FinalReview;
        if (agent?.Result is not { status: "concluido" or "parcial" } result) return false;
        var answer = new StringBuilder();
        if (result.status == "parcial") answer.AppendLine("**Parecer parcial:** há limitações que impedem uma conclusão completa.\n");
        if (run.Agents.Any(a => !a.IsFinalReview && (a.Error is not null || a.Result?.status is not "concluido")))
            answer.AppendLine("**Limitações dos especialistas:** existem análises parciais ou não concluídas. As partes sem evidências permanecem sem validação.\n");
        answer.AppendLine(result.analise);
        foreach (var finding in result.achados)
        {
            answer.AppendLine().AppendLine("- " + finding.descricao);
            foreach (var evidence in finding.evidencias) answer.AppendLine("  Evidência informada: " + evidence);
        }
        if (result.observacoes is { Count: > 0 })
        {
            answer.AppendLine().AppendLine("## Limitações e observações do parecer");
            foreach (var observation in result.observacoes) answer.AppendLine("- " + observation);
        }
        run.Answer = answer.ToString().Trim(); run.AnswerFromFinalReview = true;
        run.AnswerAuthor = agent.Developer ?? agent.Name; run.AnswerIsFallback = false;
        return true;
    }

    public static void EnsureFailureNotice(ChatRun run)
    {
        if (run.FinalReview is null || run.AnswerFromFinalReview || string.IsNullOrWhiteSpace(run.Answer) ||
            run.Answer.StartsWith("**Parecer final indisponível.", StringComparison.Ordinal)) return;
        run.Answer = "**Parecer final indisponível.** Não foi possível obter uma conclusão válida do agente do Roberto. A resposta abaixo é da SOFIA, com as informações disponíveis; não substitui o parecer solicitado.\n\n" + run.Answer;
    }
}
