using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
namespace SofiaInfis.Api.Services.A2A.Transport;
public sealed class A2AHttpClientConfiguration(A2AOptions settings, IWebHostEnvironment environment,
    ILogger<A2AHttpClientConfiguration> logger)
    : IConfigureNamedOptions<HttpClientFactoryOptions>
{
    public void Configure(HttpClientFactoryOptions options) { }
    public void Configure(string? name, HttpClientFactoryOptions options)
    {
        if (name is null || !name.StartsWith("SofiaA2A:", StringComparison.Ordinal)) return;
        var agent = settings.Agents.SingleOrDefault(x => x.IdAgente == name[9..])
            ?? throw new A2AException("autorizacao", "AGENT_NOT_REGISTERED", 403);
        var allowHttp = settings.AllowInsecureDevelopmentHttp && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
        options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = A2AHttpClientFactory.Handler(agent, allowHttp, logger));
        options.HttpClientActions.Add(client => client.Timeout = Timeout.InfiniteTimeSpan);
    }
}
