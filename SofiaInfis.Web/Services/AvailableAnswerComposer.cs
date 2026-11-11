using System.Text;

namespace SofiaInfis.Web.Services;

/// <summary>Always grounded in received results; never substitutes missing evidence with a conclusion.</summary>
public static class AvailableAnswerComposer
{
    public static void EnsureAnswer(ChatRun run)
    {
        if (!string.IsNullOrWhiteSpace(run.Answer)) return;
        run.AnswerIsFallback = true;
        var text = new StringBuilder();
        if (run.State is "cancelada" or "interrompida" or "acompanhamento_encerrado")
            text.AppendLine("A análise foi interrompida. Veja abaixo o que foi possível obter até esse momento.");
        else if (run.HasUsableResults)
            text.AppendLine("Não foi possível gerar a síntese automática. Reuni abaixo os resultados recebidos dos especialistas, com suas limitações.");
        else
            text.AppendLine("Não foi possível concluir a análise solicitada. Ainda não há resultados suficientes para responder com segurança; isso não significa que o documento esteja correto ou sem inconsistências.");

        if (run.HasUsableResults)
        {
            foreach (var agent in run.Agents.Where(a => a.Result?.status is "concluido" or "parcial"))
            {
                var result = agent.Result!;
                text.AppendLine().AppendLine("## " + agent.Name.ToUpperInvariant());
                if (result.status == "parcial") text.AppendLine("**Análise parcial — as limitações abaixo impedem uma conclusão completa.**");
                text.AppendLine(result.analise);
                foreach (var finding in result.achados)
                {
                    text.AppendLine("- " + finding.descricao);
                    foreach (var evidence in finding.evidencias) text.AppendLine("  Evidência informada: " + evidence);
                    if (finding.idDocumentos.Count > 0) text.AppendLine("  Documentos: " + string.Join(", ", finding.idDocumentos));
                }
                if (result.observacoes is not null)
                    foreach (var observation in result.observacoes) text.AppendLine("- Limitação/observação informada: " + observation);
            }
        }
        var unavailable = run.Agents.Where(a => a.Result?.status is not ("concluido" or "parcial")).ToArray();
        if (unavailable.Length > 0)
        {
            text.AppendLine().AppendLine("## O que não foi concluído");
            foreach (var agent in unavailable)
                text.AppendLine("- " + agent.Name.ToUpperInvariant() + ": não forneceu uma análise utilizável. Essa parte não foi validada.");
        }
        if (!run.HasUsableResults || run.State != "concluida" || run.Agents.Any(a => a.Result?.status == "parcial"))
            text.AppendLine().AppendLine("**Próximos passos:** confira se o arquivo contém o texto completo e os dados necessários. Complemente a pergunta com o que deseja verificar e reenvie o documento, ou tente novamente se houve indisponibilidade. Consulte também as limitações nas contribuições dos especialistas.");
        run.Answer = text.ToString().Trim();
    }
}
