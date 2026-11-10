using System.Security.Claims;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;

namespace SofiaInfis.Api.Services.A2A.Abstractions;

public interface IAgentRegistry { IReadOnlyList<AgentOptions> Agents { get; } }
public interface IAgentDiscovery { Task<IReadOnlyList<AgentDiscovery>> DiscoverAsync(ClaimsPrincipal caller, CancellationToken cancellationToken); }
public interface IA2AAgentClient
{
    Task<RemoteAgentResult> AnalyzeAsync(AgentDescriptor agent, SolicitacaoAnaliseSofia request,
        Func<string, string?, CancellationToken, Task> onTask, CancellationToken cancellationToken);
    Task TryCancelAsync(AgentDescriptor agent, string taskId, CancellationToken cancellationToken);
}
public record RemoteAgentResult(string Json, string? TaskId, string? ContextId);
public interface ISofiaAnalysisValidator
{
    ResultadoAnaliseSofia Validate(string json, SolicitacaoAnaliseSofia request, AgentDescriptor agent);
}
public interface IExecutionRepository
{
    Task InitializeAsync(CancellationToken ct);
    Task InsertExecutionAsync(ExecutionDocument execution, CancellationToken ct);
    Task<ExecutionDocument?> GetExecutionAsync(string id, CancellationToken ct);
    Task<bool> UpdateExecutionAsync(ExecutionDocument execution, long expectedVersion, CancellationToken ct);
    Task InsertCallAsync(AgentCallDocument call, CancellationToken ct);
    Task<bool> UpdateCallAsync(AgentCallDocument call, long expectedVersion, CancellationToken ct);
    Task<IReadOnlyList<AgentCallDocument>> GetCallsAsync(string executionId, CancellationToken ct);
    Task<IReadOnlyList<ExecutionDocument>> GetOpenExecutionsAsync(CancellationToken ct);
    Task SetCallExpirationAsync(string executionId, DateTime expiresAt, CancellationToken ct);
    Task PruneExpiredAsync(DateTime now, CancellationToken ct);
}
public interface IOrchestrationService
{
    Task<ExecutionDocument> StartAsync(CriarOrquestracaoRequest request, ClaimsPrincipal caller, CancellationToken ct);
    Task<ExecutionDocument> GetAsync(string id, ClaimsPrincipal caller, CancellationToken ct);
    Task<ExecutionDocument> CancelAsync(string id, ClaimsPrincipal caller, CancellationToken ct);
}
