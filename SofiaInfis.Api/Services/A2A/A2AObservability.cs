using System.Diagnostics;
using System.Diagnostics.Metrics;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Services.A2A;

public sealed class A2AObservability : IDisposable
{
    private readonly ActivitySource _source = new("SofiaInfis.A2A");
    private readonly Meter _meter = new("SofiaInfis.A2A");
    private readonly Counter<long> _failures;
    private readonly Counter<long> _calls;
    private readonly Histogram<double> _duration;
    public A2AObservability()
    {
        _failures = _meter.CreateCounter<long>("sofia.a2a.failures");
        _calls = _meter.CreateCounter<long>("sofia.a2a.calls");
        _duration = _meter.CreateHistogram<double>("sofia.a2a.call.duration", "ms");
    }
    public Activity? Begin(string operation, string execution, string? call = null, string? agent = null)
    {
        var activity = _source.StartActivity("a2a." + operation);
        activity?.SetTag("sofia.execution.id", A2ATelemetrySanitizer.ExecutionId(execution))
            .SetTag("sofia.call.id", A2ATelemetrySanitizer.ExecutionId(call)).SetTag("sofia.agent.id", agent);
        return activity;
    }
    public void Failure(string operation, string category) => _failures.Add(1, new("operation", operation), new("category", category));
    public void Observe(string agent, string state, long milliseconds, string? failureCategory = null)
    {
        _calls.Add(1, new("agent", agent), new("state", state));
        _duration.Record(milliseconds, new("agent", agent), new("state", state));
        if (failureCategory is not null)
            _failures.Add(1, new("operation", "analysis"), new("agent", agent), new("category", failureCategory));
    }
    public void Dispose() { _source.Dispose(); _meter.Dispose(); }
}
