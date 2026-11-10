using System.Security.Claims;
using System.Text.Json;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Security;
using SofiaInfis.Api.Services.A2A.Transport;

namespace SofiaInfis.Api.Services.A2A;
public sealed class AgentDiscoveryService(IAgentRegistry registry, A2AHttpClientFactory clients,
    AgentCredentialProvider credentials, A2AOptions options, IWebHostEnvironment environment) : IAgentDiscovery
{
    public async Task<IReadOnlyList<AgentDiscovery>> DiscoverAsync(ClaimsPrincipal caller, CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(options.MaxConcurrentCalls);
        // Task.WhenAll keeps registry order, while one cold card cannot delay every other agent.
        return await Task.WhenAll(registry.Agents.Select(async configured =>
        {
            await gate.WaitAsync(cancellationToken);
            try { return await DiscoverOneAsync(configured, caller, cancellationToken); }
            finally { gate.Release(); }
        }));
    }

    private async Task<AgentDiscovery> DiscoverOneAsync(AgentOptions configured, ClaimsPrincipal caller, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configured.Enabled) return Failed(configured, "AGENT_DISABLED");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.DiscoveryTimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, configured.CardUri);
            credentials.Apply(request, configured.DiscoveryCredentialRef);
            using var client = clients.Create(configured);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new A2AException("descoberta", "CARD_HTTP_FAILURE");
            var bytes = await JsonWire.ReadAsync(await response.Content.ReadAsStreamAsync(timeout.Token), options.MaxEnvelopeBytes, timeout.Token);
            using var document = JsonWire.Parse(bytes);
            var card = document.RootElement;
            if (card.GetProperty("protocolVersion").GetString() != "0.3.0" || card.GetProperty("preferredTransport").GetString() != "JSONRPC")
                throw new A2AException("protocolo", "UNSUPPORTED_PROFILE");
            if (card.GetProperty("name").GetString() != configured.ExpectedName ||
                string.IsNullOrWhiteSpace(card.GetProperty("version").GetString()) ||
                string.IsNullOrWhiteSpace(card.GetProperty("description").GetString()) ||
                card.GetProperty("capabilities").ValueKind != JsonValueKind.Object)
                throw new A2AException("descoberta", "CARD_IDENTITY_INVALID");
            bool TextMode(string property) => card.GetProperty(property).EnumerateArray()
                .Any(x => x.GetString() is "text/plain" or "text");
            if (!TextMode("defaultInputModes") || !TextMode("defaultOutputModes")) throw new A2AException("protocolo", "TEXT_MODE_REQUIRED");
            var rpc = new Uri(card.GetProperty("url").GetString()!, UriKind.Absolute);
            bool allowHttp = options.AllowInsecureDevelopmentHttp && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
            if (!AgentDestinationPolicy.IsAllowed(configured, rpc, allowHttp)) throw new A2AException("autorizacao", "RPC_DESTINATION_NOT_ALLOWED");
            if (credentials.Get(configured.CredentialRef) is null) throw new A2AException("autenticacao", "AGENT_CREDENTIAL_UNAVAILABLE");
            if (!card.TryGetProperty("securitySchemes", out var schemes) || !card.TryGetProperty("security", out var security) ||
                !security.EnumerateArray().Any(item => item.EnumerateObject().Any() && item.EnumerateObject().All(p =>
                    schemes.TryGetProperty(p.Name, out var scheme) && scheme.TryGetProperty("type", out var type) && type.GetString() == "http" &&
                    scheme.TryGetProperty("scheme", out var auth) && auth.GetString() == "bearer")))
                throw new A2AException("autenticacao", "UNSUPPORTED_AGENT_SECURITY");
            var capabilities = card.GetProperty("skills").EnumerateArray().Select(x => x.GetProperty("id").GetString()!)
                .Where(x => configured.AllowedCapabilities.Contains(x, StringComparer.Ordinal) &&
                    A2AAuthorizationPolicies.AllowsCapability(caller, x)).Distinct(StringComparer.Ordinal).ToArray();
            if (capabilities.Length == 0) return Failed(configured, "NO_AUTHORIZED_CAPABILITY");
            return new(configured.IdAgente, configured.ExpectedName, capabilities, true,
                Descriptor: new(configured, rpc, capabilities));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Failed(configured, "DISCOVERY_TIMEOUT"); }
        catch (A2AException ex) { return Failed(configured, ex.Code); }
        catch (HttpRequestException ex) when (ex.InnerException is A2AException)
            { return Failed(configured, ((A2AException)ex.InnerException).Code); }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or UriFormatException or HttpRequestException)
            { return Failed(configured, "INVALID_OR_UNAVAILABLE_CARD"); }
    }
    private static AgentDiscovery Failed(AgentOptions agent, string code) => new(agent.IdAgente, agent.ExpectedName, [], false, code);
}
