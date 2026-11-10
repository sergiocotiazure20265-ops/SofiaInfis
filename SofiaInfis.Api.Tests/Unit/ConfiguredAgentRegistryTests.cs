using SofiaInfis.Api.Services.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Tests.Unit;
public sealed class ConfiguredAgentRegistryTests
{
    private static AgentOptions Agent() => new() { IdAgente = "fiscal", ExpectedName = "Fiscal",
        CardUri = "https://agent.example/.well-known/agent-card.json",
        AllowedDestinations = ["https://agent.example/rpc"], AllowedAddresses = ["127.0.0.1"] };
    [Fact]
    public void Canonical_identity_is_preserved()
    {
        var registry = new ConfiguredAgentRegistry(new A2AOptions { Enabled = true, Agents = [Agent()] });
        Assert.Equal("fiscal", Assert.Single(registry.Agents).IdAgente);
    }
    [Fact]
    public void Duplicate_identity_is_rejected()
        => Assert.Throws<A2AException>(() => new ConfiguredAgentRegistry(new A2AOptions { Enabled = true, Agents = [Agent(), Agent()] }));
}
