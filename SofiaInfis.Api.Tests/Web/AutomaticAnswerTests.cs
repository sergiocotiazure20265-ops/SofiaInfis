using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class AutomaticAnswerTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handle(request, ct); }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private sealed class Scenario(string? status = "parcial", string mode = "ok", string? analysis = null)
    {
        public Guid Execution { get; } = Guid.NewGuid();
        private Guid Call { get; } = Guid.NewGuid();
        public int AnswerRequests { get; private set; }
        public string? AnswerBody { get; private set; }
        public string Mode { get; set; } = mode;
        public List<string> ObservedLabels { get; } = [];
        public ChatRun Run { get; } = new("Analise este documento de treinamento", ["teste.txt"]);
        public LiveSofiaChat Gateway => new(new HttpClient(new Handler(async (request, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/agentes") return Json(new { items = new[] { new AgenteDisponivel("nfse", "NFS-e", ["analisar_documentos"], true, null, "Marcos") } });
            if (path == "/api/v1/sofia/perguntar")
            {
                AnswerRequests++; AnswerBody = await request.Content!.ReadAsStringAsync(ct);
                Assert.Null(request.Headers.Authorization);
                if (Mode == "timeout") throw new TaskCanceledException("synthetic timeout");
                if (Mode == "unavailable") return new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("PRIVATE-ERROR-SENTINEL") };
                return Json(new RespostaSofia(Guid.NewGuid(), "Pergunta", Mode == "empty" ? "   " : "Resposta automática da SOFIA: há limitações nos dados recebidos.", [], [], 1, 2, 3, 50));
            }
            if (path.EndsWith("/resultado")) return Json(new ResultadoAnaliseSofia("1.0", Execution, Call, new("nfse", "NFS-e"), status!,
                analysis ?? "Não foi possível validar o prestador: falta o CNPJ.", [new("a", "Campo obrigatório ausente", ["doc"], ["CNPJ não informado"])], ["Enviar o XML completo."]));
            if (request.Method == HttpMethod.Post) return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(Execution.ToString(), "em_andamento", DateTime.UtcNow, true, "unused")) };
            return Json(new ExecucaoConsulta(Execution.ToString(), status is null ? "falhou" : status == "concluido" ? "concluida" : "parcial", DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1), ["analisar_documentos"], [],
                new([new(Call.ToString(), "nfse", status is null ? "falhou" : "concluida", status, status is not null, null,
                    status is null ? new("conexao", "AGENT_CONNECTION_FAILED", "Indisponível") : null, 62, null)], null), true));
        })), Options.Create(new SofiaOptions { ApiBaseUrl = "https://sofia.example.invalid/", ServiceToken = "synthetic-token", UseSpecialistAgents = true }), new(new ExtracaoDocumentoService()));
        public Task SubmitAsync(CancellationToken ct = default) => Gateway.SubmitAsync(new("Pessoa sintética", Run.Question,
            [new("teste.txt", Encoding.UTF8.GetBytes("Documento sintético de treinamento"))]), Run,
            () => { ObservedLabels.Add(Run.StateLabel); return Task.CompletedTask; }, ct);
    }

    [Theory]
    [InlineData("concluido")]
    [InlineData("parcial")]
    public async Task Usable_results_automatically_generate_answer_without_click_and_preserve_partial_state(string status)
    {
        var test = new Scenario(status);
        await test.SubmitAsync();
        Assert.Contains("Resposta automática", test.Run.Answer);
        Assert.Equal(1, test.AnswerRequests); Assert.False(test.Run.AnswerIsFallback);
        Assert.Equal(status == "parcial" ? "parcial" : "concluida", test.Run.State);
        Assert.Equal(test.Execution.ToString(), test.Run.ExecutionId);
        Assert.NotNull(test.Run.SynthesisExecutionId);
        Assert.Contains("Preparando resposta da SOFIA", test.ObservedLabels);
        Assert.Contains("falta o CNPJ", test.AnswerBody);
        Assert.Contains("perguntaOriginal", test.AnswerBody);
        Assert.False(test.Run.IsRunning);
    }
    [Theory]
    [InlineData("unavailable")]
    [InlineData("timeout")]
    [InlineData("empty")]
    public async Task Failed_synthesis_still_returns_received_analysis_findings_limitations_and_next_steps(string mode)
    {
        var test = new Scenario(mode: mode);
        await test.SubmitAsync();
        Assert.True(test.Run.AnswerIsFallback);
        Assert.Contains("falta o CNPJ", test.Run.Answer);
        Assert.Contains("Campo obrigatório ausente", test.Run.Answer);
        Assert.Contains("CNPJ não informado", test.Run.Answer);
        Assert.Contains("Enviar o XML completo", test.Run.Answer);
        Assert.Contains("Próximos passos", test.Run.Answer);
        Assert.DoesNotContain("PRIVATE-ERROR-SENTINEL", test.Run.Answer);
        Assert.Equal("parcial", test.Run.State); Assert.Equal(1, test.AnswerRequests);
        Assert.False(test.Run.IsSynthesizing);
    }
    [Fact]
    public async Task Oversized_synthesis_preserves_full_analysis_in_local_answer_without_calling_model()
    {
        var analysis = "INÍCIO " + new string('x', 21000) + " FIM DA ANÁLISE";
        var test = new Scenario(analysis: analysis);
        await test.SubmitAsync();
        Assert.Equal(0, test.AnswerRequests); Assert.True(test.Run.AnswerIsFallback);
        Assert.Contains(analysis, test.Run.Answer);
    }
    [Fact]
    public async Task All_agents_fail_sofia_tries_original_documents_and_keeps_failure_explicit()
    {
        var test = new Scenario(status: null);
        await test.SubmitAsync();
        Assert.Contains("os especialistas não concluíram", test.Run.Answer);
        Assert.Contains("Resposta automática", test.Run.Answer);
        Assert.Contains("Documento sintético de treinamento", test.AnswerBody);
        Assert.Equal("falhou", test.Run.State); Assert.Equal(1, test.AnswerRequests);
    }
    [Theory]
    [InlineData("unavailable")]
    [InlineData("empty")]
    public async Task All_agents_and_complementary_answer_fail_returns_explanation_instead_of_empty_answer(string mode)
    {
        var test = new Scenario(status: null, mode: mode);
        await test.SubmitAsync();
        Assert.Contains("não há resultados suficientes", test.Run.Answer);
        Assert.Contains("não significa que o documento esteja correto", test.Run.Answer);
        Assert.Contains("NFS-E", test.Run.Answer);
        Assert.Contains("Próximos passos", test.Run.Answer);
        Assert.True(test.Run.AnswerIsFallback); Assert.Equal("falhou", test.Run.State);
    }
    [Fact]
    public async Task User_cancellation_returns_explanation_and_does_not_trigger_additional_model_calls()
    {
        var test = new Scenario();
        using var ct = new CancellationTokenSource(); ct.Cancel();
        await test.SubmitAsync(ct.Token);
        Assert.Contains("interrompida", test.Run.Answer);
        Assert.Equal(0, test.AnswerRequests); Assert.False(test.Run.IsRunning);
    }
    [Fact]
    public async Task User_can_retry_synthesis_after_local_response_without_repeating_agents()
    {
        var test = new Scenario(mode: "unavailable");
        await test.SubmitAsync();
        Assert.True(test.Run.AnswerIsFallback);
        var original = Assert.Single(test.Run.Agents).Result;
        test.Mode = "ok";
        await test.Gateway.ConsolidateAsync(test.Run, () => Task.CompletedTask, default);
        Assert.False(test.Run.AnswerIsFallback); Assert.Contains("Resposta automática", test.Run.Answer);
        Assert.Same(original, Assert.Single(test.Run.Agents).Result); Assert.Equal(2, test.AnswerRequests);
    }
}
