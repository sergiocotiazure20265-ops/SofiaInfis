using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Workers;

// Requires a single live application instance; this is not a distributed lease.
public sealed class ExecutionRecoveryWorker(IExecutionRepository repository, OrchestrationQueue queue,
    TimeProvider time, A2AOptions options)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        await repository.PruneExpiredAsync(now, ct);
        foreach (var execution in await repository.GetOpenExecutionsAsync(ct))
        {
            if (execution.OwnerRunId == queue.RunId) continue;
            foreach (var call in await repository.GetCallsAsync(execution.Id, ct))
            {
                if (ExecutionStates.IsTerminal(call.Estado)) continue;
                call.Estado = ExecutionStates.Interrupted;
                call.EncerradoEm = now;
                call.FalhaTecnica = new("interrupcao", "PROCESS_INTERRUPTED", "Execução anterior interrompida sem reenvio.");
                if (!await repository.UpdateCallAsync(call, call.Version, ct)) throw new A2AException("armazenamento", "RECOVERY_CONFLICT", 503);
            }
            execution.Estado = ExecutionStates.Interrupted;
            execution.EncerradoEm = now;
            execution.ExpiresAt = now.AddDays(options.RetentionDays);
            await repository.SetCallExpirationAsync(execution.Id, execution.ExpiresAt, ct);
            if (!await repository.UpdateExecutionAsync(execution, execution.Version, ct)) throw new A2AException("armazenamento", "RECOVERY_CONFLICT", 503);
        }
    }
}
