using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class OrchestrationConcurrencyTests(MongoFixture mongo, Xunit.Abstractions.ITestOutputHelper output) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Full_capacity_returns_429_and_queued_deadline_is_not_extended()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        agent.Delay = TimeSpan.FromSeconds(10);
        using var config = new A2ATestConfiguration(mongo, agent);
        config.Settings["A2A:MaxActiveExecutions"] = "1";
        config.Settings["A2A:MaxPendingExecutions"] = "1";
        config.Settings["A2A:ExecutionTimeoutSeconds"] = "2";
        await using var factory = new SofiaApiFactory(config.Settings);
        using var client = config.Client(factory);
        var first = await PartialOrchestrationTests.Start(client);
        for (var i = 0; i < 100 && agent.AnalysisCalls == 0; i++) await Task.Delay(10);
        var second = await PartialOrchestrationTests.Start(client);
        using var full = await client.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, full.StatusCode);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        Assert.Equal("falhou", (await PartialOrchestrationTests.Wait(repository, first)).Estado);
        var queued = await PartialOrchestrationTests.Wait(repository, second);
        Assert.Equal("falhou", queued.Estado);
        Assert.Equal(TimeSpan.FromSeconds(2), queued.Deadline - queued.CriadoEm);
        Assert.True(queued.EncerradoEm <= queued.Deadline.AddSeconds(5));
        // The pending request's deadline starts at acceptance, not when a worker starts it.
        Assert.InRange(agent.AnalysisCalls, 1, 2);
    }
    [Fact]
    public async Task Five_executions_three_agents_and_two_documents_keep_all_correlations()
    {
        await using var a = await A2AAgentServer.StartAsync("a-fiscal");
        await using var b = await A2AAgentServer.StartAsync("b-contabil");
        await using var c = await A2AAgentServer.StartAsync("c-juridico");
        a.Delay = b.Delay = c.Delay = TimeSpan.FromMilliseconds(100);
        using var config = new A2ATestConfiguration(mongo, a, b, c);
        await using var factory = new SofiaApiFactory(config.Settings);
        using var client = config.Client(factory);
        for (var i = 0; i < 60; i++)
        {
            if ((await client.GetAsync("/api/v1/agentes")).IsSuccessStatusCode) break;
            await Task.Delay(50);
        }
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var submissions = await Task.WhenAll(Enumerable.Range(0, 5).Select(async index =>
        {
            using var response = await client.PostAsJsonAsync("/api/v1/orquestracoes", new
            {
                usuario = new { nome = "Pessoa sintética " + index }, pergunta = "Analise os documentos sintéticos",
                documentos = new[] { new { idDocumento = "doc-a", nome = "a.txt", texto = new string('a', 1000) }, new { idDocumento = "doc-b", nome = "b.txt", texto = new string('b', 1000) } },
                capacidades = new[] { "analisar_documentos" }
            });
            Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["idExecucao"]!.GetValue<string>();
        }));
        Assert.Equal(5, submissions.Distinct().Count());
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var durations = new List<long>();
        foreach (var id in submissions)
        {
            Assert.Equal("concluida", (await PartialOrchestrationTests.Wait(repository, id)).Estado);
            var calls = await repository.GetCallsAsync(id, default);
            Assert.Equal(3, calls.Count);
            durations.AddRange(calls.Select(call => call.DuracaoMs ?? 0));
            Assert.All(calls, call => Assert.Equal(id, JsonNode.Parse(call.ResultadoJson!)!["idExecucao"]!.GetValue<string>()));
        }
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(120));
        output.WriteLine($"SC-009: executions=5 agents=3 calls=15 documents=2 chars=1000 elapsedMs={timer.ElapsedMilliseconds} callMinMs={durations.Min()} callMaxMs={durations.Max()}");
        var firstPage = JsonNode.Parse(await client.GetStringAsync($"/api/v1/orquestracoes/{submissions[0]}?pageSize=1"))!;
        var cursor = firstPage["chamadas"]!["cursorProximo"]!.GetValue<string>();
        var secondPage = JsonNode.Parse(await client.GetStringAsync($"/api/v1/orquestracoes/{submissions[0]}?pageSize=1&cursor={Uri.EscapeDataString(cursor)}"))!;
        Assert.NotEqual(firstPage["chamadas"]!["items"]![0]!["idSolicitacao"]!.GetValue<string>(), secondPage["chamadas"]!["items"]![0]!["idSolicitacao"]!.GetValue<string>());
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/orquestracoes/{submissions[1]}?cursor={Uri.EscapeDataString(cursor)}")).StatusCode);
        foreach (var agent in new[] { a, b, c })
        {
            Assert.Equal(5, agent.AnalysisCalls);
            Assert.All(agent.Received, request => Assert.All(request["documentos"]!.AsArray(), doc => Assert.Equal(1000, doc!["texto"]!.GetValue<string>().Length)));
        }
    }
    [Fact]
    public async Task Global_deadline_closes_slow_calls_without_resending()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        agent.Delay = TimeSpan.FromSeconds(10);
        using var config = new A2ATestConfiguration(mongo, agent);
        config.Settings["A2A:ExecutionTimeoutSeconds"] = "1";
        await using var factory = new SofiaApiFactory(config.Settings);
        using var client = config.Client(factory);
        var id = await PartialOrchestrationTests.Start(client);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var execution = await PartialOrchestrationTests.Wait(repository, id);
        Assert.Equal("falhou", execution.Estado);
        Assert.True(execution.EncerradoEm <= execution.Deadline.AddSeconds(5));
        Assert.InRange(timer.Elapsed.TotalSeconds, 0, 6);
        Assert.Equal(1, agent.AnalysisCalls);
        Assert.Null(Assert.Single(await repository.GetCallsAsync(id, default)).ResultadoJson);
    }
}
