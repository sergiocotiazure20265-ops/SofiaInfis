using MongoDB.Driver;
namespace SofiaInfis.Api.Services.A2A.Persistence;

public static class MongoIndexInitializer
{
    public static async Task InitializeAsync(IMongoCollection<ExecutionDocument> executions,
        IMongoCollection<AgentCallDocument> calls, CancellationToken ct)
    {
        await executions.Indexes.CreateManyAsync([
            new(Builders<ExecutionDocument>.IndexKeys.Ascending(x => x.OwnerClientId).Ascending(x => x.CriadoEm).Ascending(x => x.Id), new CreateIndexOptions { Name = "owner_created" }),
            new(Builders<ExecutionDocument>.IndexKeys.Ascending(x => x.Estado).Ascending(x => x.OwnerRunId), new CreateIndexOptions { Name = "state_run" }),
            new(Builders<ExecutionDocument>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { Name = "expiresAt", ExpireAfter = TimeSpan.Zero })
        ], ct);
        await calls.Indexes.CreateManyAsync([
            new(Builders<AgentCallDocument>.IndexKeys.Ascending(x => x.IdExecucao).Ascending(x => x.IdAgente), new CreateIndexOptions { Name = "execution_agent", Unique = true }),
            new(Builders<AgentCallDocument>.IndexKeys.Ascending(x => x.IdExecucao).Ascending(x => x.Ordem).Ascending(x => x.Id), new CreateIndexOptions { Name = "execution_order" }),
            new(Builders<AgentCallDocument>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { Name = "expiresAt", ExpireAfter = TimeSpan.Zero })
        ], ct);
    }
}
