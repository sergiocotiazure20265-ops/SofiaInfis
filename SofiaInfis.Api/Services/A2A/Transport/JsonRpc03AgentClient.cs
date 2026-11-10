using System.Net;
using System.Text;
using System.Text.Json;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Services.A2A.Transport;

public sealed class JsonRpc03AgentClient(A2AHttpClientFactory clients, AgentCredentialProvider credentials, A2AOptions options) : IA2AAgentClient
{
    public async Task<RemoteAgentResult> AnalyzeAsync(AgentDescriptor agent, SolicitacaoAnaliseSofia request,
        Func<string, string?, CancellationToken, Task> onTask, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.CallTimeoutSeconds));
        var ct = timeout.Token;
        var rpcId = Guid.NewGuid().ToString();
        var result = await SendAsync(agent, A2A03JsonCodec.MessageRequest(request, rpcId, Guid.NewGuid().ToString()), rpcId, ct);
        string? taskId = null, contextId = null;
        while (true)
        {
            if (!result.TryGetProperty("kind", out var kind) || kind.GetString() != "task" ||
                !result.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) ||
                !result.TryGetProperty("contextId", out var context) || string.IsNullOrWhiteSpace(context.GetString()) ||
                !result.TryGetProperty("status", out var status) || !status.TryGetProperty("state", out var state))
                throw new A2AException("protocolo", "TASK_REQUIRED");
            var nextTaskId = id.GetString()!;
            var nextContextId = context.GetString()!;
            if (taskId is not null && (taskId != nextTaskId || contextId != nextContextId)) throw new A2AException("correlacao", "TASK_ID_MISMATCH");
            if (taskId is null) { taskId = nextTaskId; contextId = nextContextId; await onTask(taskId, contextId, ct); }
            switch (state.GetString())
            {
                case "completed": return new(A2A03JsonCodec.ExtractAnalysis(result), taskId, contextId);
                case "submitted": case "working": break;
                case "input-required": throw new A2AException("protocolo", "REMOTE_INPUT_REQUIRED");
                case "auth-required": throw new A2AException("autenticacao", "REMOTE_AUTH_REQUIRED");
                case "canceled": throw new A2AException("cancelamento", "REMOTE_CANCELED");
                case "failed": case "rejected": throw new A2AException("protocolo", "REMOTE_TASK_FAILED");
                default: throw new A2AException("protocolo", "UNKNOWN_TASK_STATE");
            }
            await Task.Delay(options.PollingMilliseconds, ct);
            rpcId = Guid.NewGuid().ToString();
            result = await SendAsync(agent, A2A03JsonCodec.TaskRequest("tasks/get", rpcId, taskId), rpcId, ct);
        }
    }
    public async Task TryCancelAsync(AgentDescriptor agent, string taskId, CancellationToken ct)
    {
        var rpcId = Guid.NewGuid().ToString();
        try { await SendAsync(agent, A2A03JsonCodec.TaskRequest("tasks/cancel", rpcId, taskId), rpcId, ct); }
        catch (Exception ex) when (ex is A2AException or HttpRequestException or OperationCanceledException) { /* Best effort only. */ }
    }
    private async Task<JsonElement> SendAsync(AgentDescriptor agent, string body, string rpcId, CancellationToken ct)
    {
        if (Encoding.UTF8.GetByteCount(body) > options.MaxEnvelopeBytes) throw new A2AException("validacao", "ENVELOPE_TOO_LARGE", 413);
        using var message = new HttpRequestMessage(HttpMethod.Post, agent.RpcUri) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        credentials.Apply(message, agent.Configuration.CredentialRef);
        using var client = clients.Create(agent.Configuration);
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new A2AException("autenticacao", "AGENT_UNAUTHORIZED");
        if (response.StatusCode == HttpStatusCode.Forbidden) throw new A2AException("autorizacao", "AGENT_FORBIDDEN");
        if (!response.IsSuccessStatusCode) throw new A2AException("conexao", "AGENT_HTTP_FAILURE");
        var bytes = await JsonWire.ReadAsync(await response.Content.ReadAsStreamAsync(ct), options.MaxEnvelopeBytes, ct);
        return A2A03JsonCodec.ParseResult(bytes, rpcId);
    }
}
