using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class ExecutionRaceTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Wrong_execution_is_not_accepted_as_a_valid_contribution()
    {
        await using var good = await A2AAgentServer.StartAsync("a-good");
        await using var wrong = await A2AAgentServer.StartAsync("b-wrong");
        wrong.Mode = "wrong-id";
        using var configuration = new A2ATestConfiguration(mongo, good, wrong);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        var id = await PartialOrchestrationTests.Start(client);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        Assert.Equal("parcial", (await PartialOrchestrationTests.Wait(repository, id)).Estado);
        var calls = await repository.GetCallsAsync(id, default);
        Assert.Single(calls, x => x.ResultadoJson is not null);
        Assert.Equal("correlacao", calls.Single(x => x.IdAgente == "b-wrong").FalhaTecnica!.categoria);
        var terminalCall = calls.Single(x => x.ResultadoJson is not null);
        terminalCall.Estado = "em_andamento";
        Assert.False(await repository.UpdateCallAsync(terminalCall, terminalCall.Version, default));
        var terminalRoot = (await repository.GetExecutionAsync(id, default))!;
        terminalRoot.Estado = "em_andamento";
        Assert.False(await repository.UpdateExecutionAsync(terminalRoot, terminalRoot.Version, default));
    }
    [Fact]
    public async Task Explicit_cancel_is_saved_and_late_result_cannot_reopen_execution()
    {
        await using var slow = await A2AAgentServer.StartAsync("slow");
        slow.Delay = TimeSpan.FromSeconds(5);
        using var configuration = new A2ATestConfiguration(mongo, slow);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        var id = await PartialOrchestrationTests.Start(client);
        using var response = await client.PostAsync("/api/v1/orquestracoes/" + id + "/cancelar", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        Assert.Equal("cancelada", (await PartialOrchestrationTests.Wait(repository, id)).Estado);
        await Task.Delay(100);
        Assert.Equal("cancelada", (await repository.GetExecutionAsync(id, default))!.Estado);
    }
}
