using MongoDB.Bson;
using MongoDB.Driver;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Services.A2A.Persistence;

public sealed class MongoExecutionRepository : IExecutionRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<ExecutionDocument> _executions;
    private readonly IMongoCollection<AgentCallDocument> _calls;
    public static IMongoClient CreateClient(A2AOptions options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.MongoConnectionString
            ?? throw new A2AException("armazenamento", "MONGO_NOT_CONFIGURED", 503));
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
        settings.ConnectTimeout = TimeSpan.FromSeconds(2);
        settings.SocketTimeout = TimeSpan.FromSeconds(3);
        settings.ApplicationName = "SofiaInfis.A2A";
        return new MongoClient(settings);
    }
    public MongoExecutionRepository(A2AOptions options, IMongoClient? client = null)
    {
        if (string.IsNullOrWhiteSpace(options.MongoDatabase)) throw new A2AException("armazenamento", "MONGO_DATABASE_NOT_CONFIGURED", 503);
        _database = (client ?? CreateClient(options)).GetDatabase(options.MongoDatabase)
            .WithWriteConcern(new WriteConcern(1, journal: true));
        _executions = _database.GetCollection<ExecutionDocument>("a2a_execucoes");
        _calls = _database.GetCollection<AgentCallDocument>("a2a_chamadas");
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
        await MongoIndexInitializer.InitializeAsync(_executions, _calls, ct);
    }
    public async Task InsertExecutionAsync(ExecutionDocument execution, CancellationToken ct)
    {
        try { await _executions.InsertOneAsync(execution, cancellationToken: ct); }
        catch (MongoException)
        {
            // Acknowledgment may be lost after a successful insert. Read by the same identity, never resend analysis.
            var saved = await GetExecutionAsync(execution.Id, ct);
            if (saved is null || saved.OwnerClientId != execution.OwnerClientId || saved.OwnerRunId != execution.OwnerRunId) throw;
        }
    }
    public Task<ExecutionDocument?> GetExecutionAsync(string id, CancellationToken ct) =>
        _executions.Find(x => x.Id == id).FirstOrDefaultAsync(ct)!;
    public async Task<bool> UpdateExecutionAsync(ExecutionDocument execution, long expectedVersion, CancellationToken ct)
    {
        execution.Version = expectedVersion + 1;
        var result = await _executions.ReplaceOneAsync(x => x.Id == execution.Id && x.OwnerRunId == execution.OwnerRunId &&
            x.Version == expectedVersion && x.Estado == ExecutionStates.Running, execution, new ReplaceOptions { IsUpsert = false }, ct);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }
    public async Task InsertCallAsync(AgentCallDocument call, CancellationToken ct)
    {
        if (call.ToBson().Length > 2 * 1024 * 1024) throw new A2AException("armazenamento", "CALL_RECORD_TOO_LARGE", 413);
        await _calls.InsertOneAsync(call, cancellationToken: ct);
    }
    public async Task<bool> UpdateCallAsync(AgentCallDocument call, long expectedVersion, CancellationToken ct)
    {
        if (call.ToBson().Length > 2 * 1024 * 1024) throw new A2AException("armazenamento", "CALL_RECORD_TOO_LARGE", 413);
        call.Version = expectedVersion + 1;
        var filter = Builders<AgentCallDocument>.Filter.Eq(x => x.Id, call.Id) &
            Builders<AgentCallDocument>.Filter.Eq(x => x.OwnerRunId, call.OwnerRunId) &
            Builders<AgentCallDocument>.Filter.Eq(x => x.Version, expectedVersion) &
            Builders<AgentCallDocument>.Filter.In(x => x.Estado, new[] { "aguardando", "em_andamento" });
        var result = await _calls.ReplaceOneAsync(filter, call, new ReplaceOptions { IsUpsert = false }, ct);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }
    public async Task<IReadOnlyList<AgentCallDocument>> GetCallsAsync(string executionId, CancellationToken ct) =>
        await _calls.Find(x => x.IdExecucao == executionId).SortBy(x => x.Ordem).ThenBy(x => x.Id).ToListAsync(ct);
    public async Task<IReadOnlyList<ExecutionDocument>> GetOpenExecutionsAsync(CancellationToken ct) =>
        await _executions.Find(x => x.Estado == ExecutionStates.Running).ToListAsync(ct);
    public Task SetCallExpirationAsync(string executionId, DateTime expiresAt, CancellationToken ct) =>
        _calls.UpdateManyAsync(x => x.IdExecucao == executionId,
            Builders<AgentCallDocument>.Update.Set(x => x.ExpiresAt, expiresAt), cancellationToken: ct);
    public async Task PruneExpiredAsync(DateTime now, CancellationToken ct)
    {
        await _calls.DeleteManyAsync(x => x.ExpiresAt <= now, ct);
        await _executions.DeleteManyAsync(x => x.ExpiresAt <= now, ct);
    }
}
