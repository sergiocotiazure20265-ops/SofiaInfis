using System.Security.Claims;
namespace SofiaInfis.Api.Services.A2A.Security;

public static class A2AAuthorizationPolicies
{
    public static readonly string[] Permissions = ["a2a:agents:read", "a2a:execute", "a2a:read", "a2a:cancel"];
    public static string ClientId(ClaimsPrincipal caller) =>
        caller.FindFirstValue("client_id") ?? throw new Domain.A2AException("autenticacao", "AUTHENTICATION_REQUIRED", 401);
    public static bool AllowsCapability(ClaimsPrincipal caller, string capability) =>
        caller.HasClaim("capability", capability);
}
