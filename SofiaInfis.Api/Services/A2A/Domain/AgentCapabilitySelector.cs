using System.Security.Claims;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Services.A2A.Domain;
public static class AgentCapabilitySelector
{
    public static (AgentDescriptor[] Agents, string[] Missing) Select(IReadOnlyList<AgentDiscovery> discovery,
        IReadOnlyList<string> requested, ClaimsPrincipal caller)
    {
        if (requested.Any(x => !A2AAuthorizationPolicies.AllowsCapability(caller, x)))
            throw new A2AException("autorizacao", "CAPABILITY_NOT_AUTHORIZED", 403);
        var agents = discovery.Where(x => x.elegivel && x.Descriptor is not null && x.capacidades.Intersect(requested, StringComparer.Ordinal).Any())
            .Select(x => x.Descriptor!).DistinctBy(x => x.Configuration.IdAgente).OrderBy(x => x.Configuration.IdAgente, StringComparer.Ordinal).ToArray();
        var covered = agents.SelectMany(x => x.Capabilities).ToHashSet(StringComparer.Ordinal);
        return (agents, requested.Where(x => !covered.Contains(x)).ToArray());
    }
}
