using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class SelectiveRoutingTests
{
    [Fact]
    public async Task Selective_pipeline_preserves_Postman_wire_compatibility_without_database_dependency()
    {
        var store = new InMemoryExecutionRepository();
        await PostmanSimulationTests.RunAsync(new MongoFixture(), services =>
        { services.RemoveAll<IExecutionRepository>(); services.AddSingleton<IExecutionRepository>(store); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_A2A_pipeline_calls_only_relevant_agents_and_separates_documents(bool mixed)
    {
        await using var nfse = await A2AAgentServer.StartAsync("nfse");
        await using var ecd = await A2AAgentServer.StartAsync("ecd");
        await using var irrelevant = await A2AAgentServer.StartAsync("legislacao");
        using var config = new A2ATestConfiguration(new MongoFixture(), nfse, ecd, irrelevant);
        foreach (var (i, term) in new[] { (0, "nfse"), (1, "ecd"), (2, "jurisprudencia") })
        {
            config.Settings[$"A2A:Agents:{i}:RoutingTerms:0"] = term;
            config.Settings[$"A2A:Agents:{i}:DocumentTerms:0"] = term;
        }
        var store = new InMemoryExecutionRepository();
        await using var factory = new SofiaApiFactory(config.Settings, services =>
        { services.RemoveAll<IExecutionRepository>(); services.AddSingleton<IExecutionRepository>(store); });
        using var client = config.Client(factory);
        for (var i = 0; i < 60; i++) { if ((await client.GetAsync("/api/v1/agentes")).StatusCode == HttpStatusCode.OK) break; await Task.Delay(50); }
        var documents = new List<object> { new { idDocumento = "nota", nome = "nota.xml", texto = "<Nfse>1</Nfse>" } };
        if (mixed) documents.Add(new { idDocumento = "livro", nome = "livro.txt", texto = "ECD sintética" });
        var response = await client.PostAsJsonAsync("/api/v1/orquestracoes", new { usuario = new { nome = "Teste" }, pergunta = "Analise os arquivos", documentos = documents, capacidades = new[] { "analisar_documentos" } });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var id = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["idExecucao"]!.GetValue<string>();
        var execution = await PartialOrchestrationTests.Wait(store, id);
        Assert.Equal("concluida", execution.Estado);
        Assert.Equal(1, nfse.AnalysisCalls); Assert.Equal(mixed ? 1 : 0, ecd.AnalysisCalls); Assert.Equal(0, irrelevant.AnalysisCalls);
        Assert.Equal("nota", Assert.Single(Assert.Single(nfse.Received)["documentos"]!.AsArray())!["idDocumento"]!.GetValue<string>());
        if (mixed) Assert.Equal("livro", Assert.Single(Assert.Single(ecd.Received)["documentos"]!.AsArray())!["idDocumento"]!.GetValue<string>());
        Assert.All(await store.GetCallsAsync(id, default), call => { Assert.NotNull(call.MotivoSelecao); Assert.Equal(1, call.DocumentosEnviados); });
    }

    [Fact]
    public async Task Partial_failure_preserves_selected_success_without_calling_other_specialties()
    {
        await using var first = await A2AAgentServer.StartAsync("first");
        await using var second = await A2AAgentServer.StartAsync("second");
        second.Mode = "unavailable";
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second);
        var store = new InMemoryExecutionRepository();
        await using var factory = new SofiaApiFactory(config.Settings, services =>
        { services.RemoveAll<IExecutionRepository>(); services.AddSingleton<IExecutionRepository>(store); });
        using var client = config.Client(factory);
        var id = await PartialOrchestrationTests.Start(client);
        Assert.Equal("parcial", (await PartialOrchestrationTests.Wait(store, id)).Estado);
        var calls = await store.GetCallsAsync(id, default);
        Assert.Single(calls, x => x.ResultadoJson is not null);
        Assert.Single(calls, x => x.FalhaTecnica is not null);
    }
}
