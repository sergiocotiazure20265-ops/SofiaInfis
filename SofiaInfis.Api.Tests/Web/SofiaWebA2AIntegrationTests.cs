using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using SofiaInfis.Api.Services;
using SofiaInfis.Api.Tests.Fixtures;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaWebA2AIntegrationTests(MongoFixture mongo, PostgresFixture postgres)
    : IClassFixture<MongoFixture>, IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Frontend_client_reaches_real_orchestrator_two_agents_and_separate_synthesis(bool configureCapabilities)
    {
        await using var first = await A2AAgentServer.StartAsync("fiscal");
        await using var second = await A2AAgentServer.StartAsync("contabil");
        using var configuration = new A2ATestConfiguration(mongo, first, second);
        configuration.Settings["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString;
        await using var api = new SofiaApiFactory(configuration.Settings);
        using var readyClient = configuration.Client(api);
        for (var i = 0; i < 60; i++)
        {
            if ((await readyClient.GetAsync("/api/v1/agentes")).IsSuccessStatusCode) break;
            await Task.Delay(50);
        }
        using var transport = api.CreateClient();
        var values = new Dictionary<string, string?>
        {
            ["Sofia:UseSpecialistAgents"] = "true", ["Sofia:ApiBaseUrl"] = "http://localhost/",
            ["Sofia:ServiceToken"] = configuration.Token
        };
        if (configureCapabilities) values["Sofia:Capabilities:0"] = "analisar_documentos";
        var bound = new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Sofia").Get<SofiaOptions>()!;
        var gateway = new LiveSofiaChat(transport, Options.Create(bound), new(new ExtracaoDocumentoService()));
        var input = new ChatInput("Pessoa integrada", "Analise os documentos sintéticos", [new("a.txt", Encoding.UTF8.GetBytes("Texto integral de teste A")), new("b.xml", Encoding.UTF8.GetBytes("<CompNfse><Nfse><Numero>1</Numero><Discriminacao>DOCUMENTO SINTETICO SEM VALIDADE FISCAL</Discriminacao></Nfse></CompNfse>"))]);
        var run = new ChatRun(input.Question, input.Files.Select(f => f.Name).ToArray());
        await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default);
        Assert.Equal("concluida", run.State); Assert.Equal(2, run.Agents.Count);
        var originalExecution = run.ExecutionId;
        Assert.All(run.Agents, agent => Assert.Equal(originalExecution, agent.Result!.idExecucao.ToString()));
        Assert.Equal("Pessoa integrada", first.Received.Single()["usuario"]!["nome"]!.GetValue<string>());
        await gateway.ConsolidateAsync(run, () => Task.CompletedTask, default);
        Assert.NotNull(run.Answer); Assert.NotNull(run.SynthesisExecutionId); Assert.Equal(2, run.Tokens);
        Assert.Equal(originalExecution, run.ExecutionId); Assert.Equal(1, first.AnalysisCalls); Assert.Equal(1, second.AnalysisCalls);
    }
}
