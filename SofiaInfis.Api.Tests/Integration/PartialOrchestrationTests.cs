using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class PartialOrchestrationTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    internal static async Task<string> Start(HttpClient client)
    {
        for (var i = 0; i < 60; i++)
        {
            using var ready = await client.GetAsync("/api/v1/agentes");
            if (ready.StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        using var response = await client.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["idExecucao"]!.GetValue<string>();
    }
    internal static async Task<ExecutionDocument> Wait(IExecutionRepository repository, string id)
    {
        for (int i = 0; i < 200; i++)
        {
            var execution = await repository.GetExecutionAsync(id, default);
            if (execution!.Estado != "em_andamento") return execution;
            await Task.Delay(50);
        }
        throw new TimeoutException("Synthetic execution did not finish.");
    }
    [Fact]
    public async Task Two_successes_are_preserved_when_third_agent_is_unavailable()
    {
        await using var first = await A2AAgentServer.StartAsync("a-fiscal");
        await using var second = await A2AAgentServer.StartAsync("b-contabil");
        await using var unavailable = await A2AAgentServer.StartAsync("c-unavailable");
        unavailable.Mode = "unavailable";
        using var configuration = new A2ATestConfiguration(mongo, first, second, unavailable);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        var id = await Start(client);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var execution = await Wait(repository, id);
        Assert.Equal("parcial", execution.Estado);
        var calls = await repository.GetCallsAsync(id, default);
        Assert.Equal(3, calls.Count);
        Assert.Equal(2, calls.Count(x => x.ResultadoJson is not null));
        Assert.Equal(1, second.AnalysisCalls);
        Assert.Single(calls, x => x.FalhaTecnica is not null);
    }
}
