using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class FinalReviewPipelineTests
{
    private const string Reviewer = "agente-parecer-tributario";
    private static async Task<string> Start(HttpClient client, bool two = true, bool sufficient = true)
    {
        for (var i = 0; i < 60; i++) { if ((await client.GetAsync("/api/v1/agentes")).StatusCode == HttpStatusCode.OK) break; await Task.Delay(50); }
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/Contracts/examples/roberto-lote.json")))!;
        string Data(string type) => fixture["obrigacoes"]!.AsArray().First(x => x!["tipo"]!.GetValue<string>() == type)!.ToJsonString();
        var docs = new List<object> { new { idDocumento = "nota", nome = "nfse.xml", texto = sufficient ? Data("NFSE") : "<Nfse/>" } };
        if (two) docs.Add(new { idDocumento = "livro", nome = "ecd.txt", texto = sufficient ? Data("ECD") : "ECD sintética" });
        var response = await client.PostAsJsonAsync("/api/v1/orquestracoes", new { usuario = new { nome = "Teste sintético" },
            pergunta = "Analise os arquivos e faça um parecer", documentos = docs, capacidades = new[] { "analisar_documentos" } });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["idExecucao"]!.GetValue<string>();
    }
    private static void Configure(A2ATestConfiguration config)
    {
        config.Settings["A2A:FinalReviewAgentId"] = Reviewer;
        foreach (var (i, term) in new[] { (0, "nfse"), (1, "ecd"), (2, "parecer") })
        {
            config.Settings[$"A2A:Agents:{i}:RoutingTerms:0"] = term;
            config.Settings[$"A2A:Agents:{i}:DocumentTerms:0"] = term;
        }
    }
    private static SofiaApiFactory Factory(A2ATestConfiguration config, InMemoryExecutionRepository store) =>
        new(config.Settings, services => { services.RemoveAll<IExecutionRepository>(); services.AddSingleton<IExecutionRepository>(store); });

    [Theory]
    [InlineData("success", "success")]
    [InlineData("partial", "unavailable")]
    [InlineData("unavailable", "unavailable")]
    public async Task Reviewer_runs_once_after_both_specialists_even_when_they_fail(string firstMode, string secondMode)
    {
        await using var first = await A2AAgentServer.StartAsync("nfse"); first.Mode = firstMode;
        await using var second = await A2AAgentServer.StartAsync("ecd"); second.Mode = secondMode;
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository();
        var observedOrder = false;
        final.BeforeAnalysis = async (payload, ct) =>
        {
            var calls = await store.GetCallsAsync(payload["idExecucao"]!.GetValue<string>(), ct);
            var previous = calls.Where(x => x.Etapa != FinalReviewRequestBuilder.Stage).ToArray();
            Assert.Equal(2, previous.Length); Assert.All(previous, x => Assert.True(ExecutionStates.IsTerminal(x.Estado)));
            observedOrder = true;
        };
        await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client);
        var execution = await PartialOrchestrationTests.Wait(store, id);
        var calls = await store.GetCallsAsync(id, default);
        Assert.Equal(3, calls.Count); Assert.Equal(1, final.AnalysisCalls); Assert.True(observedOrder);
        var review = Assert.Single(calls, x => x.Etapa == FinalReviewRequestBuilder.Stage);
        Assert.Equal(Reviewer, review.IdAgente); Assert.Equal(2, review.Ordem);
        Assert.All(calls.Where(x => x != review), x => Assert.True(x.EncerradoEm <= review.EnviadoEm));
        var request = Assert.Single(final.Received);
        Assert.Equal(id, request["idExecucao"]!.GetValue<string>());
        Assert.Equal(review.Id, request["idSolicitacao"]!.GetValue<string>());
        Assert.Equal(2, request["documentos"]!.AsArray().Count);
        var lote = JsonNode.Parse(request["documentos"]![0]!["texto"]!.GetValue<string>())!;
        Assert.Equal("1.0.0", lote["versaoContrato"]!.GetValue<string>());
        Assert.Equal(2, lote["obrigacoes"]!.AsArray().Count);
        var document = request["documentos"]![1]!;
        var input = JsonNode.Parse(document["texto"]!.GetValue<string>())!;
        Assert.Equal("parecer-final-sofia.v1", input["versao"]!.GetValue<string>());
        Assert.Equal("Analise os arquivos e faça um parecer", input["perguntaOriginal"]!.GetValue<string>());
        var specialists = input["especialistas"]!.AsArray(); Assert.Equal(2, specialists.Count);
        Assert.DoesNotContain(specialists, x => x!["idAgente"]!.GetValue<string>() == Reviewer);
        Assert.Equal(new[] { firstMode, secondMode }.Count(x => x == "unavailable"), specialists.Count(x => x!["falha"] is not null));
        Assert.Equal(firstMode == "success" && secondMode == "success" ? "concluida" : "parcial", execution.Estado);
        using var query = await client.GetAsync("/api/v1/orquestracoes/" + id);
        Assert.Contains("parecer_final", await query.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Missing_lote_data_skips_remote_call_and_preserves_specialist_results()
    {
        await using var first = await A2AAgentServer.StartAsync("nfse"); first.Mode = "partial";
        await using var second = await A2AAgentServer.StartAsync("ecd");
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client, sufficient: false); await PartialOrchestrationTests.Wait(store, id);
        var calls = await store.GetCallsAsync(id, default);
        Assert.Equal(2, calls.Count(x => x.ResultadoJson is not null));
        var review = Assert.Single(calls, x => x.Etapa == FinalReviewRequestBuilder.Stage);
        Assert.Equal("nao_executada", review.Estado); Assert.Null(review.EnviadoEm);
        Assert.Equal(0, review.DocumentosEnviados); Assert.Equal(0, final.AnalysisCalls);
        Assert.Equal("FINAL_REVIEW_INSUFFICIENT_DATA", review.FalhaTecnica?.codigo);
        Assert.Contains("validação de origem", review.MotivoSelecao);
    }

    [Fact]
    public async Task One_specialist_does_not_trigger_reviewer_or_count_him_as_second_agent()
    {
        await using var first = await A2AAgentServer.StartAsync("nfse");
        await using var second = await A2AAgentServer.StartAsync("ecd");
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client, two: false); await PartialOrchestrationTests.Wait(store, id);
        Assert.Single(await store.GetCallsAsync(id, default)); Assert.Equal(0, final.AnalysisCalls);
    }

    [Theory]
    [InlineData("bad-card", "FINAL_REVIEW_AGENT_UNAVAILABLE")]
    [InlineData("unavailable", "AGENT_HTTP_FAILURE")]
    [InlineData("wrong-id", "RESULT_CORRELATION_MISMATCH")]
    public async Task Reviewer_failure_is_explicit_and_preserves_specialist_results(string mode, string expected)
    {
        await using var first = await A2AAgentServer.StartAsync("nfse");
        await using var second = await A2AAgentServer.StartAsync("ecd");
        await using var final = await A2AAgentServer.StartAsync(Reviewer); final.Mode = mode;
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client); Assert.Equal("parcial", (await PartialOrchestrationTests.Wait(store, id)).Estado);
        var calls = await store.GetCallsAsync(id, default);
        Assert.Equal(2, calls.Count(x => x.ResultadoJson is not null));
        Assert.Equal(expected, Assert.Single(calls, x => x.Etapa == FinalReviewRequestBuilder.Stage).FalhaTecnica?.codigo);
    }

    [Fact]
    public async Task Oversized_review_input_is_rejected_without_silent_truncation()
    {
        await using var first = await A2AAgentServer.StartAsync("nfse"); first.AnalysisText = new string('x', 21000);
        await using var second = await A2AAgentServer.StartAsync("ecd");
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client); await PartialOrchestrationTests.Wait(store, id);
        var calls = await store.GetCallsAsync(id, default);
        Assert.Equal("FINAL_REVIEW_INPUT_TOO_LARGE", Assert.Single(calls, x => x.Etapa == FinalReviewRequestBuilder.Stage).FalhaTecnica?.codigo);
        Assert.Equal(0, final.AnalysisCalls); Assert.Contains(new string('x', 21000), calls.Single(x => x.IdAgente == "nfse").ResultadoJson);
    }

    [Fact]
    public async Task User_cancellation_does_not_start_new_final_review()
    {
        await using var first = await A2AAgentServer.StartAsync("nfse"); first.Delay = TimeSpan.FromSeconds(3);
        await using var second = await A2AAgentServer.StartAsync("ecd"); second.Delay = TimeSpan.FromSeconds(3);
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client);
        for (var i = 0; i < 100 && first.AnalysisCalls + second.AnalysisCalls < 2; i++) await Task.Delay(20);
        var response = await client.PostAsync("/api/v1/orquestracoes/" + id + "/cancelar", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(0, final.AnalysisCalls);
        Assert.Equal("cancelada", (await store.GetExecutionAsync(id, default))!.Estado);
    }

    [Fact]
    public async Task Specialist_timeouts_leave_a_reserved_budget_for_final_review()
    {
        await using var first = await A2AAgentServer.StartAsync("nfse"); first.Delay = TimeSpan.FromSeconds(5);
        await using var second = await A2AAgentServer.StartAsync("ecd"); second.Delay = TimeSpan.FromSeconds(5);
        await using var final = await A2AAgentServer.StartAsync(Reviewer);
        using var config = new A2ATestConfiguration(new MongoFixture(), first, second, final); Configure(config);
        config.Settings["A2A:ExecutionTimeoutSeconds"] = "4"; config.Settings["A2A:FinalReviewTimeoutSeconds"] = "2";
        var store = new InMemoryExecutionRepository(); await using var factory = Factory(config, store); using var client = config.Client(factory);
        var id = await Start(client); await PartialOrchestrationTests.Wait(store, id);
        Assert.Equal(1, final.AnalysisCalls);
        var calls = await store.GetCallsAsync(id, default);
        Assert.All(calls.Where(x => x.Etapa != FinalReviewRequestBuilder.Stage), x => Assert.Equal("timeout", x.Estado));
    }
}
