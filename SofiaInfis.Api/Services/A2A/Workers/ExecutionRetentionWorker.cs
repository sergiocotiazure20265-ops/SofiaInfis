using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Workers;

public sealed class ExecutionRetentionWorker(IServiceScopeFactory scopes, TimeProvider time,
    A2AModuleAvailability availability, ILogger<ExecutionRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!availability.Enabled || !availability.Valid) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (availability.Ready)
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IExecutionRepository>()
                        .PruneExpiredAsync(time.GetUtcNow().UtcDateTime, stoppingToken);
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch
            {
                logger.LogWarning("A2A_RETENTION_CLEANUP_FAILED; retention must be monitored.");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
