using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaWebGatewayTests
{
    private static LiveSofiaChat Gateway(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) =>
        new(new HttpClient(new Handler(handle)), Options.Create(new SofiaOptions { UseSpecialistAgents = true, ApiBaseUrl = "https://sofia.example.invalid/", ServiceToken = "test-service-token" }), new(new ExtracaoDocumentoService()));
    private static HttpResponseMessage Json(object data) => new(HttpStatusCode.OK) { Content = JsonContent.Create(data) };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handle(request); }
    [Fact]
    public async Task Question_without_attachments_uses_original_multipart_api_without_service_token()
    {
        var calls = 0;
        var gateway = Gateway(async request =>
        {
            calls++; Assert.Equal("/api/v1/sofia/perguntar", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Headers.Authorization);
            Assert.Contains("Pergunta sintética", await request.Content!.ReadAsStringAsync());
            return Json(new RespostaSofia(Guid.NewGuid(), "Pergunta sintética", "Resposta real do transporte simulado", [], [], 1, 2, 3, 50));
        });
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
            count++; Assert.Equal("Bearer test-service-token", request.Headers.Authorization!.ToString());
            var path = request.RequestUri!.AbsolutePath;
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
        else { await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default); Assert.Equal("parcial", run.State); Assert.True(run.HasUsableResults); Assert.Equal(2, run.Agents.Count); Assert.Null(run.Answer); }
        Assert.InRange(count, 3, 4);
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
