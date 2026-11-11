using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SofiaInfis.Api.Tests.Fixtures;

internal sealed class A2ATestConfiguration : IDisposable
{
    public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public Dictionary<string, string?> Settings { get; } = new();
    private readonly List<string> _secrets = [];
    public A2ATestConfiguration(MongoFixture mongo, params A2AAgentServer[] agents)
    {
        Settings["A2A:Enabled"] = "true";
        Settings["A2A:FinalReviewAgentId"] = "";
        Settings["A2A:AllowInsecureDevelopmentHttp"] = "true";
        Settings["A2A:MongoConnectionString"] = mongo.ConnectionString;
        Settings["A2A:MongoDatabase"] = mongo.DatabaseName;
        Settings["A2A:InboundClients:0:ClientId"] = "test-client";
        Settings["A2A:InboundClients:0:TokenHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Token)));
        var permissions = new[] { "a2a:agents:read", "a2a:execute", "a2a:read", "a2a:cancel" };
        for (int i = 0; i < permissions.Length; i++) Settings[$"A2A:InboundClients:0:Permissions:{i}"] = permissions[i];
        Settings["A2A:InboundClients:0:Capabilities:0"] = "analisar_documentos";
        for (int i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            var prefix = $"A2A:Agents:{i}:";
            var secret = "SOFIA_TEST_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
            Environment.SetEnvironmentVariable(secret, agent.Token);
            _secrets.Add(secret);
            Settings[prefix + "IdAgente"] = agent.Id;
            Settings[prefix + "ExpectedName"] = agent.Id;
            Settings[prefix + "RoutingTerms:0"] = "sintéticos";
            Settings[prefix + "DocumentTerms:0"] = "sintético";
            Settings[prefix + "CardUri"] = new Uri(agent.Origin, "/.well-known/agent-card.json").AbsoluteUri;
            Settings[prefix + "AllowedDestinations:0"] = Settings[prefix + "CardUri"];
            Settings[prefix + "AllowedDestinations:1"] = new Uri(agent.Origin, "/rpc").AbsoluteUri;
            Settings[prefix + "AllowedAddresses:0"] = "127.0.0.1";
            Settings[prefix + "AllowedCapabilities:0"] = "analisar_documentos";
            Settings[prefix + "CredentialRef"] = secret;
        }
    }
    public HttpClient Client(SofiaApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }
    public void Dispose()
    {
        foreach (var secret in _secrets) Environment.SetEnvironmentVariable(secret, null);
    }
}
