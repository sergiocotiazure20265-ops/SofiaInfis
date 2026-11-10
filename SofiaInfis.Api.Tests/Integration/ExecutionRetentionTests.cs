using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;
public sealed class ExecutionRetentionTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Expired_execution_is_hidden_and_can_be_removed_without_extending_retention()
    {
        var repository = new MongoExecutionRepository(new A2AOptions { MongoConnectionString = mongo.ConnectionString, MongoDatabase = mongo.DatabaseName });
        await repository.InitializeAsync(default);
        var clock = new ControlledTimeProvider();
        var now = clock.GetUtcNow().UtcDateTime;
        var execution = new ExecutionDocument { Id = Guid.NewGuid().ToString(), OwnerClientId = "test-client", OwnerRunId = "old",
            Estado = "concluida", CriadoEm = now, EncerradoEm = now, ExpiresAt = now.AddDays(30) };
        await repository.InsertExecutionAsync(execution, default);
        using var configuration = new A2ATestConfiguration(mongo);
        await using var factory = new SofiaApiFactory(configuration.Settings, services => services.AddSingleton<TimeProvider>(clock));
        using var client = configuration.Client(factory);
        for (int i = 0; i < 60; i++)
        {
            using var ready = await client.GetAsync("/api/v1/agentes");
            if (ready.StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/orquestracoes/" + execution.Id)).StatusCode);
        clock.Advance(TimeSpan.FromDays(31));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/orquestracoes/" + execution.Id)).StatusCode);
        await repository.PruneExpiredAsync(clock.GetUtcNow().UtcDateTime, default);
        Assert.Null(await repository.GetExecutionAsync(execution.Id, default));
        var indexes = await mongo.Database.GetCollection<BsonDocument>("a2a_execucoes").Indexes.ListAsync();
        Assert.Contains(await indexes.ToListAsync(), x => x.GetValue("name", "").AsString == "expiresAt");
    }
}
