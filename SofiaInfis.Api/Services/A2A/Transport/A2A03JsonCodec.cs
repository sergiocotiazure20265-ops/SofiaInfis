using System.Text.Json;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Transport;

public static class A2A03JsonCodec
{
    public static string MessageRequest(SolicitacaoAnaliseSofia request, string rpcId, string messageId) =>
        JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0", id = rpcId, method = "message/send",
            @params = new { message = new { kind = "message", role = "user", messageId,
                parts = new[] { new { kind = "text", text = JsonSerializer.Serialize(request, JsonWire.Options) } } } }
        }, JsonWire.Options);
    public static string TaskRequest(string method, string rpcId, string taskId) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id = rpcId, method, @params = new { id = taskId } });
    public static JsonElement ParseResult(ReadOnlyMemory<byte> bytes, string rpcId)
    {
        using var document = JsonWire.Parse(bytes);
        var root = document.RootElement;
        if (!root.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0" ||
            !root.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || id.GetString() != rpcId ||
            root.TryGetProperty("result", out var result) == root.TryGetProperty("error", out _))
            throw new A2AException("protocolo", "INVALID_RPC_ENVELOPE");
        if (root.TryGetProperty("error", out _)) throw new A2AException("protocolo", "REMOTE_RPC_ERROR");
        return result.Clone();
    }
    public static string ExtractAnalysis(JsonElement task)
    {
        if (!task.TryGetProperty("artifacts", out var artifacts) || artifacts.ValueKind != JsonValueKind.Array)
            throw new A2AException("validacao", "ANALYSIS_ARTIFACT_MISSING");
        var matches = artifacts.EnumerateArray().Where(x => x.TryGetProperty("name", out var name) && name.GetString() == "analise-sofia-v1").ToArray();
        if (matches.Length != 1 || !matches[0].TryGetProperty("artifactId", out _) || !matches[0].TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array || parts.GetArrayLength() != 1 ||
            !parts[0].TryGetProperty("kind", out var kind) || kind.GetString() != "text" ||
            !parts[0].TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
            throw new A2AException("validacao", "INVALID_ANALYSIS_ARTIFACT");
        return text.GetString()!;
    }
}
