using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaWebGatewayTests
{
    private static LiveSofiaChat Gateway(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle, bool specialists = true) =>
        new(new HttpClient(new Handler(handle)), Options.Create(new SofiaOptions { UseSpecialistAgents = specialists, ApiBaseUrl = "https://sofia.example.invalid/", ServiceToken = "test-service-token" }), new(new ExtracaoDocumentoService()));
    private static HttpResponseMessage Json(object data) => new(HttpStatusCode.OK) { Content = JsonContent.Create(data) };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handle(request); }
    [Fact]
    public async Task Configured_capabilities_are_unique_and_do_not_request_unrelated_monitoring()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sofia:UseSpecialistAgents"] = "true", ["Sofia:ApiBaseUrl"] = "https://sofia.example.invalid/",
            ["Sofia:ServiceToken"] = "synthetic-token",
            ["Sofia:Capabilities:0"] = "analisar_documentos",
            ["Sofia:Capabilities:1"] = "monitorar_legislacao_tributaria"
        }).Build();
        var bound = config.GetSection("Sofia").Get<SofiaOptions>()!;
        var execution = Guid.NewGuid();
        var posted = false;
        var gateway = new LiveSofiaChat(new HttpClient(new Handler(async request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/agentes") return Json(new { items = Array.Empty<AgenteDisponivel>() });
            if (request.RequestUri!.AbsolutePath == "/api/v1/sofia/perguntar") return Json(new RespostaSofia(Guid.NewGuid(), "Pergunta", "Resposta SOFIA", [], [], 1, 2, 3, 50));
            if (request.Method == HttpMethod.Post)
            {
                var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync())!;
                Assert.Equal(new[] { "analisar_documentos" },
                    body["capacidades"]!.AsArray().Select(x => x!.GetValue<string>()));
                Assert.True(body["documentos"]![0]!["textoCompleto"]!.GetValue<bool>());
                posted = true;
                return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(execution.ToString(), "em_andamento", DateTime.UtcNow, true, "unused")) };
            }
            return Json(new ExecucaoConsulta(execution.ToString(), "concluida", DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1), ["analisar_documentos", "monitorar_legislacao_tributaria"], [], new([], null), true));
        })), Options.Create(bound), new(new ExtracaoDocumentoService()));
        var input = new ChatInput("Pessoa sintética", "Verifique o XML sintético", [new("teste.xml", Encoding.UTF8.GetBytes("<CompNfse><Nfse><Numero>1</Numero></Nfse></CompNfse>"))]);
        await gateway.SubmitAsync(input, new ChatRun(input.Question, ["teste.xml"]), () => Task.CompletedTask, default);
        Assert.True(posted);
    }
    [Fact]
    public async Task Question_uses_original_multipart_api_without_service_token_when_specialists_disabled()
    {
        var calls = 0;
        var gateway = Gateway(async request =>
        {
            calls++; Assert.Equal("/api/v1/sofia/perguntar", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Headers.Authorization);
            Assert.Contains("Pergunta sintética", await request.Content!.ReadAsStringAsync());
            return Json(new RespostaSofia(Guid.NewGuid(), "Pergunta sintética", "Resposta real do transporte simulado", [], [], 1, 2, 3, 50));
        }, false);
        var run = new ChatRun("Pergunta sintética", []);
        await gateway.SubmitAsync(new("Pessoa", run.Question, []), run, () => Task.CompletedTask, default);
        Assert.Equal("concluida", run.State); Assert.Equal(3, run.Tokens); Assert.Contains("Resposta real", run.Answer); Assert.Empty(run.Agents); Assert.Equal(1, calls);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Documents_are_extracted_and_results_are_correlated_without_following_remote_links(bool wrongId)
    {
        var execution = Guid.NewGuid(); var call = Guid.NewGuid(); string? docId = null; var count = 0;
        var gateway = Gateway(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/sofia/perguntar") return Json(new RespostaSofia(Guid.NewGuid(), "Síntese", "Resposta consolidada", [], [], 1, 2, 3, 50));
            count++; Assert.Equal("Bearer test-service-token", request.Headers.Authorization!.ToString());
            if (path == "/api/v1/agentes") return Json(new { items = new[] { new AgenteDisponivel("fiscal", "Fiscal", ["analisar_documentos"], true, null) } });
            if (request.Method == HttpMethod.Post)
            {
                var input = JsonNode.Parse(await request.Content!.ReadAsStringAsync())!;
                Assert.Equal("Pessoa", input["usuario"]!["nome"]!.GetValue<string>());
                Assert.Equal("Texto integral sintético", input["documentos"]![0]!["texto"]!.GetValue<string>());
                docId = input["documentos"]![0]!["idDocumento"]!.GetValue<string>();
                return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(execution.ToString(), "em_andamento", DateTime.UtcNow, true, "https://evil.invalid/steal")) };
            }
            if (path.EndsWith("/resultado")) return Json(new ResultadoAnaliseSofia("1.0", wrongId ? Guid.NewGuid() : execution, call,
                new("fiscal", "Fiscal"), "concluido", "Análise sintética", [new("a", "Achado sintético", [docId!], ["Texto integral sintético"])]));
            return Json(new ExecucaoConsulta(execution.ToString(), "parcial", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddDays(30), ["analisar_documentos"], [],
                new([new(call.ToString(), "fiscal", "concluida", "concluido", true, "https://evil.invalid/steal", null, 100, null),
                    new(Guid.NewGuid().ToString(), "contabil", "falhou", null, false, null, new("conexao", "AGENT_CONNECTION_FAILED", "Safe"), 100, null)], null), true));
        });
        var run = new ChatRun("Verifique os documentos", ["a.txt"]);
        var input = new ChatInput("Pessoa", run.Question, [new("a.txt", Encoding.UTF8.GetBytes("Texto integral sintético"))]);
        if (wrongId) { await Assert.ThrowsAsync<SofiaUiException>(() => gateway.SubmitAsync(input, run, () => Task.CompletedTask, default)); Assert.False(run.HasUsableResults); }
        else { await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default); Assert.Equal("parcial", run.State); Assert.True(run.HasUsableResults); Assert.Equal(2, run.Agents.Count); Assert.Equal("Resposta consolidada", run.Answer); }
        Assert.InRange(count, 3, 4);
    }
    [Fact]
    public async Task Question_without_files_can_call_specialist_and_preserves_identity_and_selection_reason()
    {
        var execution = Guid.NewGuid(); var call = Guid.NewGuid();
        var gateway = Gateway(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/sofia/perguntar") return Json(new RespostaSofia(Guid.NewGuid(), "Síntese", "Resposta ECD consolidada", [], [], 1, 2, 3, 50));
            if (path == "/api/v1/agentes") return Json(new { items = new[] { new AgenteDisponivel("ecd", "Agente ECD", ["analisar_documentos"], true, null, "Carol") } });
            if (request.Method == HttpMethod.Post)
            {
                var input = JsonNode.Parse(await request.Content!.ReadAsStringAsync())!;
                Assert.Equal("Como validar ECD?", input["documentos"]![0]!["texto"]!.GetValue<string>());
                Assert.Equal("contexto-pergunta.txt", input["documentos"]![0]!["nome"]!.GetValue<string>());
                return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(execution.ToString(), "em_andamento", DateTime.UtcNow, true, "unused")) };
            }
            if (path.EndsWith("/resultado")) return Json(new ResultadoAnaliseSofia("1.0", execution, call, new("ecd", "Agente ECD"), "concluido", "Resposta sintética", []));
            return Json(new ExecucaoConsulta(execution.ToString(), "concluida", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), ["analisar_documentos"], [],
                new([new(call.ToString(), "ecd", "concluida", "concluido", true, null, null, 80, null, "Especialidade identificada na pergunta.", 0)], null), true));
        });
        var run = new ChatRun("Como validar ECD?", []);
        await gateway.SubmitAsync(new("Pessoa", run.Question, []), run, () => Task.CompletedTask, default);
        var agent = Assert.Single(run.Agents);
        Assert.Equal("Carol", agent.Developer);
        Assert.Equal(0, agent.DocumentsSent);
        Assert.Contains("pergunta", agent.SelectionReason);
        Assert.True(run.HasUsableResults);
    }
    [Fact]
    public async Task No_selected_agent_falls_back_to_sofia_without_broadcast()
    {
        var execution = Guid.NewGuid(); var originalCalled = false;
        var gateway = Gateway(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/agentes") return Task.FromResult(Json(new { items = Array.Empty<AgenteDisponivel>() }));
            if (path == "/api/v1/sofia/perguntar")
            {
                originalCalled = true;
                return Task.FromResult(Json(new RespostaSofia(Guid.NewGuid(), "Pergunta", "Resposta da base", [], [], 1, 2, 3, 50)));
            }
            if (request.Method == HttpMethod.Post) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(execution.ToString(), "em_andamento", DateTime.UtcNow, true, "unused")) });
            return Task.FromResult(Json(new ExecucaoConsulta(execution.ToString(), "falhou", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), ["analisar_documentos"], ["analisar_documentos"], new([], null), true)));
        });
        var run = new ChatRun("O que é a INFIS?", []);
        await gateway.SubmitAsync(new("Pessoa", run.Question, []), run, () => Task.CompletedTask, default);
        Assert.True(originalCalled); Assert.Empty(run.Agents);
        Assert.Equal("Resposta da base", run.Answer); Assert.Equal("concluida", run.State);
        Assert.Contains(run.Trace, x => x.Detail?.Contains("nenhum documento foi enviado") == true);
    }
    [Fact]
    public async Task Truncated_documents_are_rejected_instead_of_silently_delegated()
    {
        var preparation = new DocumentPreparation(new ExtracaoDocumentoService());
        await Assert.ThrowsAsync<SofiaUiException>(() => preparation.PrepareAsync([new("long.txt", Encoding.UTF8.GetBytes(new string('a', 20001)))], default));
        await Assert.ThrowsAsync<SofiaUiException>(() => preparation.PrepareAsync([new("bad.pdf", [1, 2, 3])], default));
    }
    [Fact]
    public async Task Final_synthesis_uses_original_api_and_preserves_original_execution_and_analyses()
    {
        var execution = Guid.NewGuid(); var synthesis = Guid.NewGuid();
        var run = new ChatRun("Qual a conclusão?", []) { ExecutionId = execution.ToString(), State = "parcial" };
        var result = new ResultadoAnaliseSofia("1.0", execution, Guid.NewGuid(), new("fiscal", "Fiscal"), "concluido", "Análise preservada", []);
        run.Agents.Add(new("fiscal", "Fiscal", "concluida", 30, result));
        var gateway = Gateway(async request =>
        {
            Assert.Equal("/api/v1/sofia/perguntar", request.RequestUri!.AbsolutePath);
            var body = await request.Content!.ReadAsStringAsync(); Assert.Contains("Análise preservada", body); Assert.Contains("Qual a conclusão?", body);
            return Json(new RespostaSofia(synthesis, "Consolide", "Síntese recebida", [], [], 3, 4, 7, 80));
        });
        await gateway.ConsolidateAsync(run, () => Task.CompletedTask, default);
        Assert.Equal(execution.ToString(), run.ExecutionId); Assert.Equal(synthesis.ToString(), run.SynthesisExecutionId);
        Assert.Same(result, run.Agents[0].Result); Assert.Equal("Síntese recebida", run.Answer); Assert.Equal(7, run.Tokens);
    }
    [Fact]
    public async Task Api_error_body_is_not_exposed_to_the_user()
    {
        var gateway = Gateway(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("PRIVATE-DOCUMENT-SENTINEL") }));
        var exception = await Assert.ThrowsAsync<SofiaUiException>(() => gateway.GetAgentsAsync(default));
        Assert.DoesNotContain("PRIVATE-DOCUMENT-SENTINEL", exception.Message);
    }
}
