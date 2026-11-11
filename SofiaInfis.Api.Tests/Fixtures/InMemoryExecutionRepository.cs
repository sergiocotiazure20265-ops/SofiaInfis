using System.Collections.Concurrent;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Tests.Fixtures;

// Test-only store: exercises real discovery/RPC/workers without a database dependency.
internal sealed class InMemoryExecutionRepository : IExecutionRepository
{
    private readonly ConcurrentDictionary<string, ExecutionDocument> executions = new();
    private readonly ConcurrentDictionary<string, AgentCallDocument> calls = new();
    public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
    public Task InsertExecutionAsync(ExecutionDocument execution, CancellationToken ct) { executions[execution.Id] = execution; return Task.CompletedTask; }
    public Task<ExecutionDocument?> GetExecutionAsync(string id, CancellationToken ct) => Task.FromResult(executions.GetValueOrDefault(id));
    public Task<bool> UpdateExecutionAsync(ExecutionDocument execution, long expectedVersion, CancellationToken ct)
    { lock (execution) { if (execution.Version != expectedVersion) return Task.FromResult(false); execution.Version++; executions[execution.Id] = execution; return Task.FromResult(true); } }
    public Task InsertCallAsync(AgentCallDocument call, CancellationToken ct) { calls[call.Id] = call; return Task.CompletedTask; }
    public Task<bool> UpdateCallAsync(AgentCallDocument call, long expectedVersion, CancellationToken ct)
    { lock (call) { if (call.Version != expectedVersion) return Task.FromResult(false); call.Version++; calls[call.Id] = call; return Task.FromResult(true); } }
    public Task<IReadOnlyList<AgentCallDocument>> GetCallsAsync(string executionId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AgentCallDocument>>(calls.Values.Where(x => x.IdExecucao == executionId).OrderBy(x => x.Ordem).ToArray());
    public Task<IReadOnlyList<ExecutionDocument>> GetOpenExecutionsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ExecutionDocument>>([]);
    public Task SetCallExpirationAsync(string executionId, DateTime expiresAt, CancellationToken ct) => Task.CompletedTask;
    public Task PruneExpiredAsync(DateTime now, CancellationToken ct) => Task.CompletedTask;
}
