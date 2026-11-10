using SofiaInfis.Api.Services.A2A.Workers;
namespace SofiaInfis.Api.Services.A2A.Domain;
// Compatibility facade for the first increment; execution now handles all selected specialists.
public sealed class SingleAgentExecutionPipeline(AgentExecutionPipeline pipeline)
{
    public Task RunAsync(OrchestrationWork work, CancellationToken ct) => pipeline.RunAsync(work, ct);
}
