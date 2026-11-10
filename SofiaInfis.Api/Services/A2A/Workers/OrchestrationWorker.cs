using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Workers;

public sealed class OrchestrationWorker(IServiceScopeFactory scopes, OrchestrationQueue queue,
    A2AOptions options, A2AModuleAvailability availability, ILogger<OrchestrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!availability.Enabled || !availability.Valid) return;
        try
        {
            while (!availability.Ready) await Task.Delay(50, stoppingToken);
            await Task.WhenAll(Enumerable.Range(0, options.MaxActiveExecutions).Select(_ => ConsumeAsync(stoppingToken)));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
    private async Task ConsumeAsync(CancellationToken ct)
    {
        await foreach (var work in queue.Channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                if (Interlocked.CompareExchange(ref work.Started, 1, 0) != 0)
                {
                    await work.Finished.Task;
                    continue;
                }
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<SingleAgentExecutionPipeline>().RunAsync(work, ct);
            }
            catch
            {
                logger.LogWarning("A2A_EXECUTION_STORAGE_FAILURE execution {ExecutionId}", work.Execution.Id);
                work.Finished.TrySetResult(false);
            }
            finally { queue.Complete(work); }
        }
    }
}
