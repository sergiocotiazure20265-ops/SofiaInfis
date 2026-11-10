using SofiaInfis.Api.Services.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;
public sealed class NoEligibleAgentExecutionTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Empty_selection_is_registered_as_failed_without_invented_analysis()
    {
        var options = new A2AOptions { MongoConnectionString = mongo.ConnectionString, MongoDatabase = mongo.DatabaseName };
        var repository = new MongoExecutionRepository(options);
        await repository.InitializeAsync(default);
        var execution = new ExecutionDocument { Id = Guid.NewGuid().ToString(), OwnerClientId = "owner", OwnerRunId = "run", ExpiresAt = DateTime.UtcNow.AddDays(30) };
        await repository.InsertExecutionAsync(execution, default);
        await new NoEligibleAgentExecutionRecorder(repository, TimeProvider.System, options).CompleteAsync(execution, ["missing"], default);
        Assert.Equal("falhou", (await repository.GetExecutionAsync(execution.Id, default))!.Estado);
        Assert.Empty(await repository.GetCallsAsync(execution.Id, default));
    }
}
