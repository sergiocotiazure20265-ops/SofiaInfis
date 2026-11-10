using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A;

public sealed class ConfiguredAgentRegistry : IAgentRegistry
{
    public IReadOnlyList<AgentOptions> Agents { get; }
    public ConfiguredAgentRegistry(A2AOptions options)
    {
        if (!A2AOptionsValidator.IsValid(options)) throw new A2AException("validacao", "INVALID_A2A_CONFIGURATION", 503);
        Agents = Array.AsReadOnly(options.Agents);
    }
}
