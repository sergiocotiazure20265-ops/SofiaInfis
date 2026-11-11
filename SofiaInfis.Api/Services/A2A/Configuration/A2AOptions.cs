namespace SofiaInfis.Api.Services.A2A.Configuration;

public sealed class A2AOptions
{
    public bool Enabled { get; set; }
    public bool AllowInsecureDevelopmentHttp { get; set; }
    public int DiscoveryTimeoutSeconds { get; set; } = 5;
    public int CallTimeoutSeconds { get; set; } = 60;
    public int ExecutionTimeoutSeconds { get; set; } = 120;
    public int FinalizationTimeoutSeconds { get; set; } = 5;
    public int PollingMilliseconds { get; set; } = 500;
    public int MaxEnvelopeBytes { get; set; } = 1024 * 1024;
    public int MaxActiveExecutions { get; set; } = 5;
    public int MaxPendingExecutions { get; set; } = 10;
    public int MaxConcurrentCalls { get; set; } = 3;
    public int RetentionDays { get; set; } = 30;
    public string? MongoConnectionString { get; set; }
    public string? MongoDatabase { get; set; }
    public AgentOptions[] Agents { get; set; } = [];
    public InboundClientOptions[] InboundClients { get; set; } = [];
    public string[] AllowedBrowserOrigins { get; set; } = [];
}
public sealed class AgentOptions
{
    public string IdAgente { get; set; } = "";
    public string ExpectedName { get; set; } = "";
    public string? Developer { get; set; }
    public string[] RoutingTerms { get; set; } = [];
    public string[] DocumentTerms { get; set; } = [];
    public string CardUri { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string[] AllowedDestinations { get; set; } = [];
    public string[] AllowedAddresses { get; set; } = [];
    public string[] AllowedCapabilities { get; set; } = [];
    public string? CredentialRef { get; set; }
    public string? DiscoveryCredentialRef { get; set; }
}
public sealed class InboundClientOptions
{
    public string ClientId { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string[] Permissions { get; set; } = [];
    public string[] Capabilities { get; set; } = [];
}
