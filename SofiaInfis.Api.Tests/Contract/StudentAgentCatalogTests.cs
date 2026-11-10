using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Security;

namespace SofiaInfis.Api.Tests.Contract;

public sealed class StudentAgentCatalogTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SofiaInfis.Api"));

    [Fact]
    public void Catalog_appends_without_replacing_existing_agents_or_duplicate_ids()
    {
        var existing = new AgentOptions { IdAgente = "nfse-validador", Enabled = false, ExpectedName = "explicit override" };
        var options = new A2AOptions { Agents = [new() { IdAgente = "existing" }, existing] };
        StudentAgentCatalog.Append(options, Root);
        StudentAgentCatalog.Append(options, Root);
        Assert.Equal(9, options.Agents.Length);
        Assert.Same(existing, options.Agents.Single(x => x.IdAgente == existing.IdAgente));
    }

    [Fact]
    public void Reviewed_catalog_requires_exact_https_destinations_bearer_references_and_scoped_capabilities()
    {
        var options = new A2AOptions();
        StudentAgentCatalog.Append(options, Root);
        Assert.Equal(8, options.Agents.Length);
        Assert.All(options.Agents, agent =>
        {
            Assert.Equal("SOFIA_A2A_TOKEN", agent.CredentialRef);
            Assert.Equal(agent.CredentialRef, agent.DiscoveryCredentialRef);
            Assert.Equal(agent.IdAgente == "agente-monitoramento-legislacao"
                ? ["analisar_documentos", "monitorar_legislacao_tributaria"] : ["analisar_documentos"], agent.AllowedCapabilities);
            Assert.Equal(2, agent.AllowedDestinations.Length);
            Assert.True(AgentDestinationPolicy.IsAllowed(agent, new Uri(agent.CardUri), false));
            Assert.All(agent.AllowedDestinations, uri => Assert.StartsWith("https://", uri));
            Assert.False(AgentDestinationPolicy.IsAllowed(agent, new Uri(new Uri(agent.CardUri), "/other"), false));
            Assert.False(AgentDestinationPolicy.IsAllowedAddress(agent, System.Net.IPAddress.Loopback));
        });
    }
}
