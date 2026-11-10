using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Security;

public sealed class ExecutionCursorProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("SofiaInfis.A2A.Cursor.v1");
    public string Encode(string owner, string execution, int offset) => _protector.Protect(JsonSerializer.Serialize(new Cursor(owner, execution, offset)));
    public int Decode(string? cursor, string owner, string execution)
    {
        if (cursor is null) return 0;
        if (cursor.Length > 4096) throw new A2AException("validacao", "INVALID_CURSOR");
        try
        {
            var value = JsonSerializer.Deserialize<Cursor>(_protector.Unprotect(cursor));
            if (value is null || value.Owner != owner || value.Execution != execution || value.Offset < 0)
                throw new A2AException("validacao", "INVALID_CURSOR");
            return value.Offset;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
            { throw new A2AException("validacao", "INVALID_CURSOR"); }
    }
    private sealed record Cursor(string Owner, string Execution, int Offset);
}
