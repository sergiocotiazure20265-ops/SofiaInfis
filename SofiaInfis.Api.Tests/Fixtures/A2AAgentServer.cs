using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SofiaInfis.Api.Tests.Fixtures;

internal sealed class A2AAgentServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentDictionary<string, JsonObject> _tasks = new();
    public string Id { get; }
    public Uri Origin { get; private set; } = null!;
    public int AnalysisCalls;
    public int PollCalls;
    public int CancelCalls;
    public string Mode { get; set; } = "success";
    public TimeSpan Delay { get; set; }
    public Func<CancellationToken, Task>? BeforeCard { get; set; }
    public Func<JsonObject, CancellationToken, Task>? BeforeAnalysis { get; set; }
    public string? AnalysisText { get; set; }
    public string Token { get; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    public ConcurrentQueue<JsonObject> Received { get; } = new();
    private A2AAgentServer(string id)
    {
        Id = id;
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
        _app = builder.Build();
        _app.MapGet("/.well-known/agent-card.json", async (HttpContext context) =>
        {
            if (BeforeCard is not null) await BeforeCard(context.RequestAborted);
            return Results.Json(new
        {
            protocolVersion = Mode == "bad-card" ? "1.0" : "0.3.0",
            preferredTransport = "JSONRPC", name = Id, version = "1.0.0", description = "Synthetic specialist",
            url = new Uri(Origin, "/rpc").AbsoluteUri,
            capabilities = new { streaming = false, pushNotifications = false },
            defaultInputModes = new[] { "text/plain" }, defaultOutputModes = new[] { "text/plain" },
            skills = new[] { new { id = "analisar_documentos", name = "Analysis", description = "Synthetic analysis", tags = new[] { "test" } } },
            securitySchemes = new { sofiaService = new { type = "http", scheme = "bearer" } },
            security = new[] { Mode == "unsupported-security" ? new Dictionary<string, string[]> { ["sofiaService"] = [], ["unsupportedOAuth"] = [] } : new Dictionary<string, string[]> { ["sofiaService"] = [] } }
            });
        });
        _app.MapPost("/rpc", (Delegate)HandleAsync);
    }
    public static async Task<A2AAgentServer> StartAsync(string id = "agente-fiscal")
    {
        var server = new A2AAgentServer(id);
        await server._app.StartAsync();
        server.Origin = new Uri(server._app.Urls.Single());
        return server;
    }
    private async Task<IResult> HandleAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization != "Bearer " + Token) return Results.Unauthorized();
        var rpc = (await JsonNode.ParseAsync(context.Request.Body))!.AsObject();
        string method = rpc["method"]!.GetValue<string>();
        if (method == "tasks/cancel") { Interlocked.Increment(ref CancelCalls); return Results.Json(new { jsonrpc = "2.0", id = rpc["id"]!.DeepClone(), result = new { kind = "task", id = rpc["params"]!["id"]!.GetValue<string>(), contextId = "context-test", status = new { state = "canceled" } } }); }
        if (method == "tasks/get")
        {
            Interlocked.Increment(ref PollCalls);
            var request = _tasks[rpc["params"]!["id"]!.GetValue<string>()];
            return Finish(rpc, request);
        }
        if (method != "message/send") return Results.StatusCode(501);
        Interlocked.Increment(ref AnalysisCalls);
        if (Mode == "unavailable") return Results.StatusCode(503);
        var payload = JsonNode.Parse(rpc["params"]!["message"]!["parts"]![0]!["text"]!.GetValue<string>())!.AsObject();
        Received.Enqueue((JsonObject)payload.DeepClone());
        if (BeforeAnalysis is not null) await BeforeAnalysis(payload, context.RequestAborted);
        if (Delay > TimeSpan.Zero) await Task.Delay(Delay, context.RequestAborted);
        if (Mode == "working")
        {
            _tasks["task-test"] = payload;
            return Results.Json(new { jsonrpc = "2.0", id = rpc["id"]!.DeepClone(), result = new { kind = "task", id = "task-test", contextId = "context-test", status = new { state = "working" } } });
        }
        return Finish(rpc, payload);
    }
    private IResult Finish(JsonObject rpc, JsonObject payload)
    {
        var result = new JsonObject
        {
            ["versaoContrato"] = "1.0", ["idExecucao"] = payload["idExecucao"]!.DeepClone(),
            ["idSolicitacao"] = payload["idSolicitacao"]!.DeepClone(),
            ["agente"] = new JsonObject { ["idAgente"] = Id, ["nome"] = Id },
            ["status"] = Mode == "business-error" ? "erro" : Mode == "partial" ? "parcial" : "concluido",
            ["analise"] = AnalysisText ?? "Synthetic analysis; no factual assertion about a real document.",
            ["achados"] = new JsonArray()
        };
        if (Mode == "wrong-id") result["idExecucao"] = Guid.NewGuid().ToString();
        if (Mode == "business-error") result["erro"] = new JsonObject { ["codigo"] = "SEM_EVIDENCIA", ["mensagem"] = "Synthetic business error." };
        return Results.Json(new { jsonrpc = "2.0", id = rpc["id"]!.DeepClone(), result = new
        {
            kind = "task", id = "task-test", contextId = "context-test", status = new { state = "completed" },
            artifacts = new[] { new { artifactId = "artifact-test", name = "analise-sofia-v1", parts = new[] { new { kind = "text", text = result.ToJsonString() } } } }
        } });
    }
    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
