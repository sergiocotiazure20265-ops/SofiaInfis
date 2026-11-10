using System.Security.Claims;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class AgentDiscoveryTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Address_outside_exact_allowlist_remains_blocked_and_reports_sanitized_code()
    {
        await using var agent = await A2AAgentServer.StartAsync("blocked");
        using var configuration = new A2ATestConfiguration(mongo, agent);
        configuration.Settings["A2A:Agents:0:AllowedAddresses:0"] = "127.0.0.2";
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var scope = factory.Services.CreateScope();
        var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("capability", "analisar_documentos")], "test"));
        var card = Assert.Single(await scope.ServiceProvider.GetRequiredService<IAgentDiscovery>().DiscoverAsync(caller, default));
        Assert.False(card.elegivel);
        Assert.Equal("DESTINATION_ADDRESS_NOT_ALLOWED", card.motivo);
        Assert.Equal(0, agent.AnalysisCalls);
    }

    [Theory]
    [InlineData("bad-card")]
    [InlineData("unsupported-security")]
    public async Task Valid_cards_are_eligible_but_incompatible_cards_are_not_and_no_analysis_is_sent(string mode)
    {
        await using var valid = await A2AAgentServer.StartAsync("fiscal");
        await using var invalid = await A2AAgentServer.StartAsync("invalid");
        invalid.Mode = mode;
        using var configuration = new A2ATestConfiguration(mongo, valid, invalid);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var scope = factory.Services.CreateScope();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("capability", "analisar_documentos")], "test"));
        var discovery = scope.ServiceProvider.GetRequiredService<IAgentDiscovery>();
        var cards = await discovery.DiscoverAsync(user, default);
        Assert.True(cards.Single(x => x.idAgente == "fiscal").elegivel);
        Assert.False(cards.Single(x => x.idAgente == "invalid").elegivel);
        Assert.Equal(0, valid.AnalysisCalls + invalid.AnalysisCalls);
    }
}
