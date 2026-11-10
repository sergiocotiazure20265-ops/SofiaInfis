using System.Security.Claims;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Services.A2A.Security;
using SofiaInfis.Api.Services.A2A.Workers;
namespace SofiaInfis.Api.Services.A2A;

public sealed class OrchestrationService(IExecutionRepository repository, OrchestrationQueue queue,
    OrchestrationInputValidator validator, A2AOptions options, TimeProvider time, ExecutionCoordinator coordinator) : IOrchestrationService
{
    public async Task<ExecutionDocument> StartAsync(CriarOrquestracaoRequest request, ClaimsPrincipal caller, CancellationToken ct)
    {
        validator.Validate(request);
        if (request.capacidades.Any(c => !A2AAuthorizationPolicies.AllowsCapability(caller, c)))
            throw new A2AException("autorizacao", "CAPABILITY_NOT_AUTHORIZED", 403);
        if (!queue.Reserve()) throw new A2AException("capacidade", "EXECUTION_CAPACITY_EXCEEDED", 429);
        var accepted = false;
        try
        {
            var now = time.GetUtcNow().UtcDateTime;
            var execution = new ExecutionDocument
            {
                Id = Guid.NewGuid().ToString(), OwnerClientId = A2AAuthorizationPolicies.ClientId(caller),
                OwnerRunId = queue.RunId, CriadoEm = now, Deadline = now.AddSeconds(options.ExecutionTimeoutSeconds),
                ExpiresAt = now.AddDays(options.RetentionDays).AddSeconds(options.ExecutionTimeoutSeconds + options.FinalizationTimeoutSeconds),
                DocumentoIds = request.documentos.Select(x => x.idDocumento).ToArray(),
                CapacidadesSolicitadas = request.capacidades.ToArray()
            };
            await repository.InsertExecutionAsync(execution, ct);
            var work = new OrchestrationWork(execution, request, caller, time);
            if (!queue.Enqueue(work))
            {
                work.Cancellation.Dispose(); work.Gate.Dispose();
                execution.Estado = ExecutionStates.Failed; execution.FalhaRegistro = "QUEUE_UNAVAILABLE";
                execution.EncerradoEm = now; execution.ExpiresAt = now.AddDays(options.RetentionDays);
                using var finalize = new CancellationTokenSource(TimeSpan.FromSeconds(options.FinalizationTimeoutSeconds));
                await repository.UpdateExecutionAsync(execution, 0, finalize.Token);
                throw new A2AException("capacidade", "QUEUE_UNAVAILABLE", 503);
            }
            accepted = true;
            return execution;
        }
        finally { if (!accepted) queue.Release(); }
    }
    public async Task<ExecutionDocument> GetAsync(string id, ClaimsPrincipal caller, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out _)) throw new A2AException("validacao", "EXECUTION_NOT_FOUND", 404);
        var execution = await repository.GetExecutionAsync(id, ct);
        if (execution is null || execution.OwnerClientId != A2AAuthorizationPolicies.ClientId(caller) ||
            execution.ExpiresAt <= time.GetUtcNow().UtcDateTime) throw new A2AException("autorizacao", "EXECUTION_NOT_FOUND", 404);
        return execution;
    }
    public async Task<ExecutionDocument> CancelAsync(string id, ClaimsPrincipal caller, CancellationToken ct)
    {
        var execution = await GetAsync(id, caller, ct);
        if (ExecutionStates.IsTerminal(execution.Estado)) return execution;
        if (!queue.Items.TryGetValue(id, out var work)) throw new A2AException("interrupcao", "EXECUTION_NOT_ACTIVE", 503);
        work.CancelRequested = true;
        try { work.Cancellation.Cancel(); } catch (ObjectDisposedException) { }
        if (Interlocked.CompareExchange(ref work.Started, -1, 0) == 0)
            await coordinator.CloseAsync(repository, work, false);
        if (!await work.Finished.Task.WaitAsync(TimeSpan.FromSeconds(options.FinalizationTimeoutSeconds), ct))
            throw new A2AException("armazenamento", "CANCEL_NOT_CONFIRMED", 503);
        return await GetAsync(id, caller, ct);
    }
}
