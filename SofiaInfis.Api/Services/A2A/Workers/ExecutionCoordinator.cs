using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Services.A2A.Workers;

public sealed class ExecutionCoordinator(A2AOptions options, TimeProvider time)
{
    public CancellationTokenSource FinalizationToken(OrchestrationWork work)
    {
        var left = work.Execution.Deadline.AddSeconds(options.FinalizationTimeoutSeconds) - time.GetUtcNow().UtcDateTime;
        var budget = TimeSpan.FromSeconds(options.FinalizationTimeoutSeconds);
        return new CancellationTokenSource(left <= TimeSpan.Zero ? TimeSpan.Zero : left < budget ? left : budget);
    }
    public async Task<bool> SaveCallAsync(IExecutionRepository repository, OrchestrationWork work,
        AgentCallDocument call, CancellationToken ct)
    {
        await work.Gate.WaitAsync(ct);
        try
        {
            if (work.Closed) return false;
            return await repository.UpdateCallAsync(call, call.Version, ct);
        }
        finally { work.Gate.Release(); }
    }
    public async Task<bool> CloseAsync(IExecutionRepository repository, OrchestrationWork work, bool interrupted)
    {
        using var finish = FinalizationToken(work);
        var ct = finish.Token;
        bool saved = false;
        bool locked = false;
        try
        {
            await work.Gate.WaitAsync(ct); locked = true;
            if (work.Closed) return false;
            var execution = work.Execution;
            var calls = await repository.GetCallsAsync(execution.Id, ct);
            foreach (var call in calls.Where(x => !ExecutionStates.IsTerminal(x.Estado)))
            {
                call.Estado = interrupted ? "interrompida" : work.CancelRequested ? "cancelada" : execution.FalhaRegistro is not null ? "falhou" : "timeout";
                call.FalhaTecnica = new(call.Estado == "timeout" ? "timeout" : call.Estado == "cancelada" ? "cancelamento" : call.Estado == "falhou" ? "armazenamento" : "interrupcao",
                    "CALL_" + call.Estado.ToUpperInvariant(), "A chamada foi encerrada sem análise válida.");
                call.EncerradoEm = time.GetUtcNow().UtcDateTime;
                if (!await repository.UpdateCallAsync(call, call.Version, ct)) throw new A2AException("armazenamento", "CALL_CLOSE_CONFLICT", 503);
            }
            execution.Estado = interrupted ? ExecutionStates.Interrupted : work.CancelRequested ? ExecutionStates.Canceled :
                ExecutionResultReducer.Reduce(calls, execution.SemCobertura.Length > 0);
            execution.EncerradoEm = time.GetUtcNow().UtcDateTime;
            execution.ExpiresAt = execution.EncerradoEm.Value.AddDays(options.RetentionDays);
            work.Closed = true;
            await repository.SetCallExpirationAsync(execution.Id, execution.ExpiresAt, ct);
            saved = await repository.UpdateExecutionAsync(execution, execution.Version, ct);
            return saved;
        }
        finally
        {
            if (locked) work.Gate.Release();
            work.Finished.TrySetResult(saved);
        }
    }
}
