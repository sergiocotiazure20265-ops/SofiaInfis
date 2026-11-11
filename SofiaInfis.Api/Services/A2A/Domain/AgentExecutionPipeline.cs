using System.Diagnostics;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Services.A2A.Workers;
namespace SofiaInfis.Api.Services.A2A.Domain;

public sealed class AgentExecutionPipeline(IExecutionRepository repository, IAgentDiscovery discovery,
    IA2AAgentClient client, ISofiaAnalysisValidator validator, A2AOptions options, TimeProvider time,
    ExecutionCoordinator coordinator, A2AObservability telemetry)
{
    public async Task RunAsync(OrchestrationWork work, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(work.Cancellation.Token, stoppingToken);
        var ct = linked.Token;
        var execution = work.Execution;
        using var activity = telemetry.Begin("execution", execution.Id);
        try
        {
            ct.ThrowIfCancellationRequested();
            execution.IniciadoEm = time.GetUtcNow().UtcDateTime;
            if (!await repository.UpdateExecutionAsync(execution, execution.Version, ct))
                throw new A2AException("armazenamento", "EXECUTION_WRITE_CONFLICT", 503);
            var selection = AgentCapabilitySelector.Select(await discovery.DiscoverAsync(work.Caller, ct), work.Request!.capacidades, work.Caller);
            var routes = AgentRequestRouter.Select(selection.Agents, work.Request);
            var covered = routes.SelectMany(x => x.Agent.Capabilities).ToHashSet(StringComparer.Ordinal);
            execution.SemCobertura = work.Request.capacidades.Where(x => !covered.Contains(x)).ToArray();
            execution.AgentesSelecionados = routes.Select(x => x.Agent.Configuration.IdAgente).ToArray();
            if (!await repository.UpdateExecutionAsync(execution, execution.Version, ct)) throw new A2AException("armazenamento", "EXECUTION_WRITE_CONFLICT", 503);
            var pending = new List<(AgentRoute Route, AgentCallDocument Call)>();
            foreach (var route in routes)
            {
                var call = new AgentCallDocument
                {
                    Id = Guid.NewGuid().ToString(), IdExecucao = execution.Id, IdAgente = route.Agent.Configuration.IdAgente,
                    MotivoSelecao = route.Reason,
                    DocumentosEnviados = route.Documents.Count(x => x.idDocumento != AgentRequestRouter.QuestionContextId),
                    Ordem = pending.Count, OwnerRunId = execution.OwnerRunId, CriadoEm = time.GetUtcNow().UtcDateTime,
                    Deadline = execution.Deadline, ExpiresAt = execution.ExpiresAt
                };
                await repository.InsertCallAsync(call, ct);
                pending.Add((route, call));
            }
            using var slots = new SemaphoreSlim(options.MaxConcurrentCalls);
            await Task.WhenAll(pending.Select(x => ExecuteCallAsync(work, x.Route, x.Call, slots, ct)));
        }
        catch (OperationCanceledException) { }
        catch
        {
            execution.FalhaRegistro = "EXECUTION_PROCESSING_FAILED";
            telemetry.Failure("execution", "processing");
        }
        finally
        {
            var saved = await coordinator.CloseAsync(repository, work, stoppingToken.IsCancellationRequested);
            if (!saved) telemetry.Failure("execution", "storage");
        }
    }
    private async Task ExecuteCallAsync(OrchestrationWork work, AgentRoute route, AgentCallDocument call,
        SemaphoreSlim slots, CancellationToken ct)
    {
        bool acquired = false;
        var watch = Stopwatch.StartNew();
        using var activity = telemetry.Begin("analysis", work.Execution.Id, call.Id, call.IdAgente);
        try
        {
            await slots.WaitAsync(ct); acquired = true;
            ct.ThrowIfCancellationRequested();
            call.EnviadoEm = time.GetUtcNow().UtcDateTime;
            call.Deadline = call.EnviadoEm.Value.AddSeconds(options.CallTimeoutSeconds) < work.Execution.Deadline
                ? call.EnviadoEm.Value.AddSeconds(options.CallTimeoutSeconds) : work.Execution.Deadline;
            call.Estado = ExecutionStates.Running;
            if (!await coordinator.SaveCallAsync(repository, work, call, ct)) throw new A2AException("armazenamento", "CALL_WRITE_CONFLICT", 503);
            var request = OrchestrationInputValidator.DomainRequest(work.Request! with { documentos = route.Documents }, Guid.Parse(work.Execution.Id), Guid.Parse(call.Id));
            var remote = await client.AnalyzeAsync(route.Agent, request, async (taskId, contextId, token) =>
            {
                call.TaskId = taskId; call.ContextId = contextId;
                if (!await coordinator.SaveCallAsync(repository, work, call, token)) throw new A2AException("armazenamento", "CALL_WRITE_CONFLICT", 503);
            }, ct);
            validator.Validate(remote.Json, request, route.Agent);
            call.ResultadoJson = remote.Json; call.Estado = ExecutionStates.Completed;
        }
        catch (OperationCanceledException)
        {
            call.Estado = work.CancelRequested ? "cancelada" : "timeout";
            call.FalhaTecnica = new(work.CancelRequested ? "cancelamento" : "timeout", "CALL_CANCELED_OR_TIMED_OUT", "Espera de análise encerrada.");
            if (call.TaskId is not null)
            {
                using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                await client.TryCancelAsync(route.Agent, call.TaskId, cancel.Token);
            }
        }
        catch (A2AException ex) { call.Estado = ExecutionStates.Failed; call.FalhaTecnica = ex.Failure; }
        catch (HttpRequestException) { call.Estado = ExecutionStates.Failed; call.FalhaTecnica = new("conexao", "AGENT_CONNECTION_FAILED", "Agente indisponível."); }
        catch { call.Estado = ExecutionStates.Failed; call.FalhaTecnica = new("armazenamento", "CALL_PROCESSING_FAILED", "Não foi possível registrar a análise."); }
        finally
        {
            if (acquired) slots.Release();
            call.EncerradoEm = time.GetUtcNow().UtcDateTime; call.DuracaoMs = watch.ElapsedMilliseconds;
            telemetry.Observe(call.IdAgente, call.Estado, call.DuracaoMs.Value, call.FalhaTecnica?.categoria);
            using var save = coordinator.FinalizationToken(work);
            if (!await coordinator.SaveCallAsync(repository, work, call, save.Token))
                throw new A2AException("armazenamento", "CALL_FINAL_WRITE_CONFLICT", 503);
        }
    }
}
