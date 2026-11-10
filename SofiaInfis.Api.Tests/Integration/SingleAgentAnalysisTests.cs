using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class SingleAgentAnalysisTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    internal static object Request => new
    {
        usuario = new { nome = "Pessoa Sintética" }, pergunta = "Verifique os documentos sintéticos",
        documentos = new[] { new { idDocumento = "doc-a", nome = "a.txt", texto = "Texto sintético A" },
            new { idDocumento = "doc-b", nome = "b.txt", texto = "Texto sintético B" } },
        capacidades = new[] { "analisar_documentos" }
    };
    [Theory]
    [InlineData("success", "concluida")]
    [InlineData("working", "concluida")]
    [InlineData("business-error", "falhou")]
    public async Task Text_is_sent_once_and_original_result_is_saved(string mode, string expected)
    {
        await using var agent = await A2AAgentServer.StartAsync();
        agent.Mode = mode;
        using var configuration = new A2ATestConfiguration(mongo, agent);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        for (int i = 0; i < 40; i++)
        {
            using var ready = await client.GetAsync("/api/v1/agentes");
            if (ready.StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        using var response = await client.PostAsJsonAsync("/api/v1/orquestracoes", Request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var accepted = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var id = accepted["idExecucao"]!.GetValue<string>();
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var execution = await repository.GetExecutionAsync(id, default);
        for (int i = 0; i < 100 && execution!.Estado == "em_andamento"; i++)
        {
            await Task.Delay(50);
            execution = await repository.GetExecutionAsync(id, default);
        }
        Assert.Equal(expected, execution!.Estado);
        var call = Assert.Single(await repository.GetCallsAsync(id, default));
        Assert.NotNull(call.ResultadoJson);
        Assert.Equal(1, agent.AnalysisCalls);
        var sent = Assert.Single(agent.Received);
        Assert.Equal("Texto sintético B", sent["documentos"]![1]!["texto"]!.GetValue<string>());
        Assert.Equal(id, sent["idExecucao"]!.GetValue<string>());
        if (mode == "working") Assert.True(agent.PollCalls > 0);
    }
}
