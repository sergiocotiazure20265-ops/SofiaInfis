using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;
public sealed class ExecutionRestartTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Completed_execution_is_read_after_restart_without_new_remote_call()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var configuration = new A2ATestConfiguration(mongo, agent);
        string id;
        await using (var first = new SofiaApiFactory(configuration.Settings))
        {
            using var client = configuration.Client(first);
            id = await PartialOrchestrationTests.Start(client);
            using var scope = first.Services.CreateScope();
            await PartialOrchestrationTests.Wait(scope.ServiceProvider.GetRequiredService<IExecutionRepository>(), id);
        }
        await using var restarted = new SofiaApiFactory(configuration.Settings);
        using var second = configuration.Client(restarted);
        for (int i = 0; i < 60; i++)
        {
            using var ready = await second.GetAsync("/api/v1/agentes");
            if (ready.StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        var json = JsonNode.Parse(await second.GetStringAsync("/api/v1/orquestracoes/" + id))!;
        Assert.Equal("concluida", json["estado"]!.GetValue<string>());
        Assert.Equal(1, agent.AnalysisCalls);
    }
    [Fact]
    public async Task Abandoned_execution_is_interrupted_without_resending_payload()
    {
        var repository = new MongoExecutionRepository(new A2AOptions { MongoConnectionString = mongo.ConnectionString, MongoDatabase = mongo.DatabaseName });
        await repository.InitializeAsync(default);
        var execution = new ExecutionDocument { Id = Guid.NewGuid().ToString(), OwnerClientId = "test-client", OwnerRunId = "previous-process",
            CriadoEm = DateTime.UtcNow, Deadline = DateTime.UtcNow.AddSeconds(120), ExpiresAt = DateTime.UtcNow.AddDays(30) };
        await repository.InsertExecutionAsync(execution, default);
        await repository.InsertCallAsync(new() { Id = Guid.NewGuid().ToString(), IdExecucao = execution.Id, IdAgente = "previous-agent",
            OwnerRunId = "previous-process", ExpiresAt = execution.ExpiresAt }, default);
        using var configuration = new A2ATestConfiguration(mongo);
        await using var restarted = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(restarted);
        for (int i = 0; i < 60; i++)
        {
            var saved = await repository.GetExecutionAsync(execution.Id, default);
            _ = await client.GetAsync("/api/v1/agentes");
            if (saved!.Estado == "interrompida") break;
            await Task.Delay(50);
        }
        Assert.Equal("interrompida", (await repository.GetExecutionAsync(execution.Id, default))!.Estado);
        Assert.Equal("interrompida", (await repository.GetCallsAsync(execution.Id, default))[0].Estado);
    }
}
