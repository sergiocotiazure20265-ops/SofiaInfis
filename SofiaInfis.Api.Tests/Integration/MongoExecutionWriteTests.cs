using MongoDB.Bson;
using MongoDB.Driver;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class MongoExecutionWriteTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Execution_and_call_are_durable_and_terminal_writes_are_immutable()
    {
        var options = new A2AOptions { MongoConnectionString = mongo.ConnectionString, MongoDatabase = mongo.DatabaseName };
        var repository = new MongoExecutionRepository(options);
        await repository.InitializeAsync(default);
        var execution = new ExecutionDocument { Id = Guid.NewGuid().ToString(), OwnerClientId = "owner", OwnerRunId = "run", CriadoEm = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
        await repository.InsertExecutionAsync(execution, default);
        Assert.Equal("owner", (await repository.GetExecutionAsync(execution.Id, default))?.OwnerClientId);
        var call = new AgentCallDocument { Id = Guid.NewGuid().ToString(), IdExecucao = execution.Id, IdAgente = "agent", OwnerRunId = "run", ExpiresAt = execution.ExpiresAt };
        await repository.InsertCallAsync(call, default);
        call.Estado = "concluida";
        Assert.True(await repository.UpdateCallAsync(call, 0, default));
        Assert.False(await repository.UpdateCallAsync(call, 0, default));
        Assert.False(await repository.UpdateCallAsync(call, 1, default));
        var calls = await repository.GetCallsAsync(execution.Id, default);
        Assert.Single(calls);
        Assert.Equal("concluida", calls[0].Estado);
    }
}
