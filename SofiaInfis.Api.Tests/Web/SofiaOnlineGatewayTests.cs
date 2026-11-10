using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SofiaInfis.Web.Services;
using Xunit.Abstractions;

namespace SofiaInfis.Api.Tests.Web;

public sealed class OnlineGatewayFactAttribute : FactAttribute
{
    public OnlineGatewayFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SOFIA_TEST_ONLINE_GATEWAY") != "1")
            Skip = "Opt-in: frontend DI against the authorized online API, synthetic XML only.";
    }
}

public sealed class SofiaOnlineGatewayTests(ITestOutputHelper output)
{
    [OnlineGatewayFact]
    public async Task Configured_frontend_gateway_submits_synthetic_xml_to_published_api()
    {
        var access = Environment.GetEnvironmentVariable("SOFIA_TEST_ONLINE_ACCESS_KEY");
        var token = Environment.GetEnvironmentVariable("SOFIA_TEST_ONLINE_SERVICE_TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(access));
        Assert.False(string.IsNullOrWhiteSpace(token));
        await using var factory = new WebApplicationFactory<SofiaInfis.Web.Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sofia:EmbeddedApi"] = "false", ["Sofia:DemoMode"] = "false",
                ["Sofia:UseSpecialistAgents"] = "true",
                ["Sofia:ApiBaseUrl"] = "https://sofia-infis.onrender.com/",
                ["Sofia:ServiceToken"] = token, ["Deployment:AccessKey"] = access,
                ["Sofia:Capabilities:0"] = "analisar_documentos",
                ["Sofia:Capabilities:1"] = "monitorar_legislacao_tributaria"
            }));
        });
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ISofiaChat>();
        var input = new ChatInput("Teste sintético SOFIA", "Verifique a estrutura deste XML sintético. Apresente campos ausentes e limitações.",
            [new("teste.xml", Encoding.UTF8.GetBytes("<CompNfse><Nfse><Numero>1</Numero><Discriminacao>TESTE SOFIA-A2A-002, SEM VALIDADE FISCAL</Discriminacao></Nfse></CompNfse>"))]);
        var run = new ChatRun(input.Question, ["teste.xml"]);
        await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default);
        Assert.True(Guid.TryParse(run.ExecutionId, out _));
        Assert.False(run.IsRunning);
        Assert.True(run.HasUsableResults);
        Assert.All(run.Agents.Where(a => a.Result is not null), agent =>
            Assert.Equal(run.ExecutionId, agent.Result!.idExecucao.ToString()));
        var report = System.Text.Json.JsonSerializer.Serialize(new
        {
            run.ExecutionId, run.State, agents = run.Agents.Select(a => new { a.Id, a.State, a.Error, domainStatus = a.Result?.status })
        });
        output.WriteLine(report);
        if (Environment.GetEnvironmentVariable("SOFIA_TEST_ONLINE_GATEWAY_REPORT") is { Length: > 0 } path)
            await File.WriteAllTextAsync(path, report);
    }
}
