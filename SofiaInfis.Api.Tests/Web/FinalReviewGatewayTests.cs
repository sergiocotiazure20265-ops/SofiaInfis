using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class FinalReviewGatewayTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handle(request); }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    [Theory]
    [InlineData("concluido")]
    [InlineData("parcial")]
    [InlineData("erro")]
    [InlineData(null)]
    public async Task Final_review_is_presented_with_actual_authorship_or_explicit_sofia_contingency(string? finalStatus)
    {
        var execution = Guid.NewGuid(); var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var names = new[] { "nfse", "ecd", "agente-parecer-tributario" }; var legacy = 0;
        var gateway = new LiveSofiaChat(new HttpClient(new Handler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/agentes") return Json(new { items = names.Select(x => new AgenteDisponivel(x, x, ["analisar_documentos"], true, null, x == names[2] ? "Roberto Rabelo" : "Autor")) });
            if (path == "/api/v1/sofia/perguntar") { legacy++; return Json(new RespostaSofia(Guid.NewGuid(), "Pergunta", "Resposta de contingência da SOFIA", [], [], 1, 2, 3, 50)); }
            if (request.Method == HttpMethod.Post) return new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new OrquestracaoAceita(execution.ToString(), "em_andamento", DateTime.UtcNow, true, "unused")) };
            if (path.EndsWith("/resultado"))
            {
                var i = Array.FindIndex(ids, id => path.Contains(id.ToString()));
                return Json(new ResultadoAnaliseSofia("1.0", execution, ids[i], new(names[i], names[i]), i == 2 ? finalStatus! : "concluido",
                    i == 2 ? "Conclusão recebida do Roberto" : "Análise original preservada", [],
                    i == 2 ? ["Revisão humana necessária"] : [],
                    i == 2 && finalStatus == "erro" ? new("LOTE_AUSENTE", "Contrato final ainda não suportado") : null));
            }
            await Task.CompletedTask;
            return Json(new ExecucaoConsulta(execution.ToString(), finalStatus == "concluido" ? "concluida" : "parcial", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), ["analisar_documentos"], [],
                new(names.Select((x, i) => new ChamadaResumo(ids[i].ToString(), x, i == 2 && finalStatus is null ? "falhou" : "concluida", i == 2 ? finalStatus : "concluido",
                    i != 2 || finalStatus is not null, null, i == 2 && finalStatus is null ? new("conexao", "AGENT_HTTP_FAILURE", "Indisponível") : null, 10, null,
                    etapa: i == 2 ? "parecer_final" : null)).ToArray(), null), true));
        })), Options.Create(new SofiaOptions { UseSpecialistAgents = true, ServiceToken = "synthetic", ApiBaseUrl = "https://example.invalid/" }), new(new ExtracaoDocumentoService()));
        var run = new ChatRun("Analise os documentos", []);
        await gateway.SubmitAsync(new("Teste", run.Question, []), run, () => Task.CompletedTask, default);
        Assert.Equal(3, run.Agents.Count); Assert.Single(run.Agents, x => x.IsFinalReview);
        Assert.Equal("Análise original preservada", run.Agents[0].Result!.analise);
        if (finalStatus is "concluido" or "parcial")
        {
            Assert.True(run.AnswerFromFinalReview); Assert.Equal("Roberto Rabelo", run.AnswerAuthor);
            Assert.Contains("Conclusão recebida do Roberto", run.Answer); Assert.Contains("Revisão humana necessária", run.Answer);
            Assert.Equal(0, legacy);
            if (finalStatus == "parcial") Assert.Contains("Parecer parcial", run.Answer);
        }
        else
        {
            Assert.False(run.AnswerFromFinalReview); Assert.Contains("Parecer final indisponível", run.Answer);
            Assert.Contains("contingência da SOFIA", run.Answer); Assert.Equal(1, legacy);
        }
    }

    [Fact]
    public void Known_agent_colors_are_distinct_stable_and_safe_for_html_class_names()
    {
        var ids = new[] { "nfse-validador", "agente-validacao-ecd", "agente-ecf", "agente-fiscal", "agente-auditoria-nfe", "agente-dimp", "agente-monitoramento-legislacao", "agente-parecer-tributario" };
        Assert.Equal(8, ids.Select(AgentPalette.ClassFor).Distinct().Count());
        Assert.Equal(AgentPalette.ClassFor("custom"), AgentPalette.ClassFor("custom"));
        Assert.Matches("^agent-color-[0-7]$", AgentPalette.ClassFor("\"><script>alert(1)</script>"));
    }
}
