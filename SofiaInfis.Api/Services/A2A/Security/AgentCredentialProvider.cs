using System.Net.Http.Headers;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Security;

public sealed class AgentCredentialProvider
{
    public string? Get(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (!System.Text.RegularExpressions.Regex.IsMatch(reference, "^[A-Z][A-Z0-9_]{0,127}$"))
            throw new A2AException("autenticacao", "INVALID_CREDENTIAL_REFERENCE");
        var secret = Environment.GetEnvironmentVariable(reference);
        if (string.IsNullOrWhiteSpace(secret) || secret.Contains('\r') || secret.Contains('\n'))
            throw new A2AException("autenticacao", "AGENT_CREDENTIAL_UNAVAILABLE", 503);
        return secret;
    }
    public void Apply(HttpRequestMessage request, string? reference)
    {
        var secret = Get(reference);
        if (secret is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
    }
}
