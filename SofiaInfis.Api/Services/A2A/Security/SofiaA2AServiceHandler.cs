using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.Services.A2A.Configuration;

namespace SofiaInfis.Api.Services.A2A.Security;

public static class ServiceTokenValidator
{
    public static InboundClientOptions? Validate(string token, A2AOptions options)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 1024) return null;
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        InboundClientOptions? match = null;
        foreach (var client in options.InboundClients)
        {
            if (client.TokenHash.Length != 64 || !client.TokenHash.All(Uri.IsHexDigit)) continue;
            if (CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(client.TokenHash))) match = client;
        }
        return match;
    }
}
public sealed class SofiaA2AServiceHandler(IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger, UrlEncoder encoder, A2AOptions settings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "SofiaA2AService";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        var client = ServiceTokenValidator.Validate(header[7..].Trim(), settings);
        if (client is null) return Task.FromResult(AuthenticateResult.Fail("INVALID_SERVICE_CREDENTIAL"));
        var claims = new List<Claim> { new("client_id", client.ClientId) };
        claims.AddRange(client.Permissions.Select(p => new Claim("permission", p)));
        claims.AddRange(client.Capabilities.Select(c => new Claim("capability", c)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = "Bearer";
        await WriteFailureAsync(401, "AUTHENTICATION_REQUIRED");
    }
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return WriteFailureAsync(403, "PERMISSION_DENIED");
    }
    private Task WriteFailureAsync(int status, string code) => Response.WriteAsJsonAsync(
        new { status, title = "Acesso não autorizado.", codigo = code, traceId = Context.TraceIdentifier },
        options: null, contentType: "application/problem+json", cancellationToken: Context.RequestAborted);
}
