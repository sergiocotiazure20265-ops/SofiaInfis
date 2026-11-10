using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Tests.Unit;
public sealed class OutboundTransportPolicyTests
{
    private static AgentOptions Agent => new() { AllowedDestinations = ["https://agent.example/rpc"], AllowedAddresses = ["127.0.0.1"] };
    [Theory]
    [InlineData("https://evil.example/rpc")]
    [InlineData("https://agent.example/other")]
    [InlineData("https://agent.example/rpc?token=secret")]
    [InlineData("https://user:secret@agent.example/rpc")]
    [InlineData("http://agent.example/rpc")]
    public void Only_explicit_destination_without_secret_is_eligible(string url)
        => Assert.False(AgentDestinationPolicy.IsAllowed(Agent, new Uri(url), false));
    [Fact]
    public void Explicit_https_destination_is_eligible()
        => Assert.True(AgentDestinationPolicy.IsAllowed(Agent, new Uri("https://agent.example/rpc"), false));
}
