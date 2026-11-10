using System.Text.Json;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Services.A2A.Transport;
public static class JsonWire
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    public static async Task<byte[]> ReadAsync(Stream stream, int limit, CancellationToken ct)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            int count = await stream.ReadAsync(buffer, ct);
            if (count == 0) break;
            if (output.Length + count > limit) throw new A2AException("validacao", "ENVELOPE_TOO_LARGE", 413);
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }
    public static JsonDocument Parse(ReadOnlyMemory<byte> bytes)
    {
        JsonDocument document;
        try { document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 }); }
        catch (JsonException) { throw new A2AException("validacao", "INVALID_JSON"); }
        try { RejectDuplicates(document.RootElement); return document; }
        catch { document.Dispose(); throw; }
    }
    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new A2AException("validacao", "DUPLICATE_JSON_PROPERTY");
                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicates(item);
    }
}
