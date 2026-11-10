using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Services.A2A.Workers;

public sealed class OrchestrationQueue
{
    private readonly SemaphoreSlim _capacity;
    public Channel<OrchestrationWork> Channel { get; }
    public string RunId { get; } = Guid.NewGuid().ToString();
    public ConcurrentDictionary<string, OrchestrationWork> Items { get; } = new();
    public OrchestrationQueue(A2AOptions options)
    {
        _capacity = new(Math.Max(1, options.MaxActiveExecutions + options.MaxPendingExecutions));
        Channel = System.Threading.Channels.Channel.CreateBounded<OrchestrationWork>(
            new BoundedChannelOptions(Math.Max(1, options.MaxPendingExecutions)) { FullMode = BoundedChannelFullMode.Wait });
    }
    public bool Reserve() => _capacity.Wait(0);
    public void Release() => _capacity.Release();
    public bool Enqueue(OrchestrationWork item)
    {
        if (!Items.TryAdd(item.Execution.Id, item)) return false;
        if (Channel.Writer.TryWrite(item)) return true;
        Items.TryRemove(item.Execution.Id, out _);
        return false;
    }
    public void Complete(OrchestrationWork item)
    {
        Items.TryRemove(item.Execution.Id, out _);
        item.Request = null;
        item.Cancellation.Dispose();
        item.Gate.Dispose();
        Release();
    }
}
public sealed class OrchestrationWork
{
    public ExecutionDocument Execution { get; }
    public CriarOrquestracaoRequest? Request { get; set; }
    public ClaimsPrincipal Caller { get; }
    public CancellationTokenSource Cancellation { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public bool CancelRequested;
    public int Started;
    public bool Closed;
    public TaskCompletionSource<bool> Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public OrchestrationWork(ExecutionDocument execution, CriarOrquestracaoRequest request, ClaimsPrincipal caller, TimeProvider time)
    {
        Execution = execution; Request = request;
        Caller = new ClaimsPrincipal(new ClaimsIdentity(caller.Claims, caller.Identity?.AuthenticationType));
        var remaining = execution.Deadline - time.GetUtcNow().UtcDateTime;
        Cancellation = new CancellationTokenSource(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }
}
