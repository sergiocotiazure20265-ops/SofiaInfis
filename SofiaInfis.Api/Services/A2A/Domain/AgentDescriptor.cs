using SofiaInfis.Api.Services.A2A.Configuration;

namespace SofiaInfis.Api.Services.A2A.Domain;
public record AgentDescriptor(AgentOptions Configuration, Uri RpcUri, string[] Capabilities);
public record AgentDiscovery(string idAgente, string nome, string[] capacidades, bool elegivel,
    string? motivo = null, AgentDescriptor? Descriptor = null);
