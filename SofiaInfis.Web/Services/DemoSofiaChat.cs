using SofiaInfis.Api.DTOs.A2A;
namespace SofiaInfis.Web.Services;

// Explicit Development-only preview. Never selected as a fallback for API failures.
public sealed class DemoSofiaChat : ISofiaChat
{
    public bool IsDemo => true;
    public Task<IReadOnlyList<AgenteDisponivel>> GetAgentsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AgenteDisponivel>>(
        [new("fiscal", "Fiscal", ["analisar_documentos"], true, null), new("contabil", "Contábil", ["analisar_documentos"], true, null), new("tributario", "Tributário", ["analisar_documentos"], true, null)]);
    public async Task SubmitAsync(ChatInput input, ChatRun run, Func<Task> changed, CancellationToken ct)
    {
        DocumentPreparation.Validate(input.Files);
        run.ExecutionId = Guid.NewGuid().ToString(); run.State = "em_andamento";
        run.Trace.Add(new(DateTimeOffset.UtcNow, "Demonstração iniciada", "Dados simulados; nenhum agente real ou modelo foi chamado.")); await changed();
        try
        {
            if (input.Files.Count > 0)
                foreach (var agent in await GetAgentsAsync(ct))
                {
                    run.Agents.Add(new(agent.idAgente, agent.nome, "em_andamento", null)); await changed();
                    await Task.Delay(650, ct);
                    var result = new ResultadoAnaliseSofia("1.0", Guid.Parse(run.ExecutionId), Guid.NewGuid(), new(agent.idAgente, agent.nome), "concluido",
                        $"**Exemplo de contribuição — {agent.nome}**\n\nNesta área você verá a análise enviada pelo especialista, com os achados e as referências aos documentos. Este conteúdo é simulado e não representa uma avaliação dos arquivos anexados.", []);
                    run.Agents[^1] = new(agent.idAgente, agent.nome, "concluida", 650, result);
                    run.Trace.Add(new(DateTimeOffset.UtcNow, "Contribuição simulada recebida", agent.nome)); await changed();
                }
            else
            {
                await Task.Delay(700, ct);
                run.Answer = "Olá! Sou a **SOFIA**, sua parceira em inteligência fiscal.\n\nEste é o modo de demonstração da interface. Na conexão real, você poderá tirar dúvidas com a SOFIA ou anexar documentos para receber análises dos especialistas.\n\nPara conhecer a apresentação das análises, adicione um PDF ou TXT e envie uma pergunta.";
            }
            run.State = "concluida";
        }
        catch (OperationCanceledException) { run.State = "cancelada"; }
        finally { run.EndedAt = DateTimeOffset.UtcNow; await changed(); }
    }
    public async Task ConsolidateAsync(ChatRun run, Func<Task> changed, CancellationToken ct)
    {
        run.IsSynthesizing = true; await changed();
        try
        {
            await Task.Delay(700, ct);
            run.Answer = "**Uma visão integrada dos especialistas**\n\nNa conexão real, a SOFIA reunirá as contribuições em uma síntese com os principais pontos, limitações e próximos passos.\n\nAs análises de cada agente continuam disponíveis abaixo. Você também pode abrir o rastreamento para consultar os identificadores e as etapas da execução.\n\nEste resultado é demonstrativo. Nenhum documento foi analisado por agentes reais.";
            run.Trace.Add(new(DateTimeOffset.UtcNow, "Síntese demonstrativa apresentada"));
        }
        finally { run.IsSynthesizing = false; await changed(); }
    }
}
