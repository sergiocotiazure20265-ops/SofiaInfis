using System.Text;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.Services;
using SofiaInfis.Api.Tests.Fixtures;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaWebA2AIntegrationTests(MongoFixture mongo, PostgresFixture postgres)
    : IClassFixture<MongoFixture>, IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Frontend_client_reaches_real_orchestrator_two_agents_and_separate_synthesis()
    {
        await using var first = await A2AAgentServer.StartAsync("fiscal");
        await using var second = await A2AAgentServer.StartAsync("contabil");
        using var configuration = new A2ATestConfiguration(mongo, first, second);
        configuration.Settings["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString;
        await using var api = new SofiaApiFactory(configuration.Settings);
        using var readyClient = configuration.Client(api);
        for (var i = 0; i < 60; i++)
        {
            if ((await readyClient.GetAsync("/api/v1/agentes")).IsSuccessStatusCode) break;
            await Task.Delay(50);
        }
        using var transport = api.CreateClient();
        var gateway = new LiveSofiaChat(transport, Options.Create(new SofiaOptions { UseSpecialistAgents = true, ApiBaseUrl = "http://localhost/", ServiceToken = configuration.Token }), new(new ExtracaoDocumentoService()));
        var input = new ChatInput("Pessoa integrada", "Analise os documentos sintéticos", [new("a.txt", Encoding.UTF8.GetBytes("Texto integral de teste A")), new("b.txt", Encoding.UTF8.GetBytes("Texto integral de teste B"))]);
        var run = new ChatRun(input.Question, input.Files.Select(f => f.Name).ToArray());
        await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default);
        Assert.Equal("concluida", run.State); Assert.Equal(2, run.Agents.Count);
        var originalExecution = run.ExecutionId;
        Assert.All(run.Agents, agent => Assert.Equal(originalExecution, agent.Result!.idExecucao.ToString()));
        Assert.Equal("Pessoa integrada", first.Received.Single()["usuario"]!["nome"]!.GetValue<string>());
        await gateway.ConsolidateAsync(run, () => Task.CompletedTask, default);
        Assert.NotNull(run.Answer); Assert.NotNull(run.SynthesisExecutionId); Assert.Equal(2, run.Tokens);
        Assert.Equal(originalExecution, run.ExecutionId); Assert.Equal(1, first.AnalysisCalls); Assert.Equal(1, second.AnalysisCalls);
    }
}
