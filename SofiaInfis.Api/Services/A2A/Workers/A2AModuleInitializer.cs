using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Workers;
public sealed class A2AModuleInitializer(IServiceScopeFactory scopes, A2AModuleAvailability availability,
    ILogger<A2AModuleInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!availability.Enabled || !availability.Valid) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IExecutionRepository>().InitializeAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<ExecutionRecoveryWorker>().RunAsync(stoppingToken);
                availability.Ready = true;
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch
            {
                availability.Ready = false;
                logger.LogWarning("A2A_STORAGE_UNAVAILABLE; legacy endpoints remain enabled.");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
