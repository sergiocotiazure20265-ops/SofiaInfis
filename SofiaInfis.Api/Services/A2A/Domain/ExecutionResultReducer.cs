using System.Text.Json;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Services.A2A.Domain;
public static class ExecutionResultReducer
{
    public static string Reduce(IReadOnlyList<AgentCallDocument> calls, bool missing)
    {
        var statuses = calls.Select(call =>
        {
            if (call.Estado != ExecutionStates.Completed || call.ResultadoJson is null) return "";
            try { using var result = JsonDocument.Parse(call.ResultadoJson); return result.RootElement.GetProperty("status").GetString() ?? ""; }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return ""; }
        }).ToArray();
        if (!statuses.Any(x => x is "concluido" or "parcial")) return ExecutionStates.Failed;
        return !missing && statuses.All(x => x == "concluido") ? ExecutionStates.Completed : ExecutionStates.Partial;
    }
}
