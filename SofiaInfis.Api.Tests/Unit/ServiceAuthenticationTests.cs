using System.Security.Cryptography;
using System.Text;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Tests.Unit;

public sealed class ServiceAuthenticationTests
{
    [Fact]
    public void Valid_token_resolves_stable_client_without_using_user_name()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var options = new A2AOptions { InboundClients = [new() { ClientId = "integration-client", TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))) }] };
        Assert.Equal("integration-client", ServiceTokenValidator.Validate(token, options)?.ClientId);
        var rotated = token + "-rotated";
        options.InboundClients[0].TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rotated)));
        Assert.Null(ServiceTokenValidator.Validate(token, options));
        Assert.Equal("integration-client", ServiceTokenValidator.Validate(rotated, options)?.ClientId);
    }
    [Fact]
    public void Unknown_or_empty_credential_is_rejected()
    {
        Assert.Null(ServiceTokenValidator.Validate("", new()));
        Assert.Null(ServiceTokenValidator.Validate("unknown", new()));
    }
}
