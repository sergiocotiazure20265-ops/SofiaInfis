using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SofiaInfis.Web.Components.Shared;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Api.Tests.Web;

public sealed class AgentTraceRenderingTests
{
    [Theory]
    [InlineData("concluido", "Concluído", "success")]
    [InlineData("parcial", "Parcial", "warning")]
    [InlineData("erro", "Erro na análise", "warning")]
    public void Trace_status_reflects_business_result(string status, string label, string tone)
    {
        var result = new SofiaInfis.Api.DTOs.A2A.ResultadoAnaliseSofia("1.0", Guid.NewGuid(), Guid.NewGuid(), new("nfse", "NFS-e"), status, "Teste", []);
        var agent = new AgentView("nfse", "NFS-e", "concluida", 10, result);
        Assert.Equal(label, agent.StatusLabel); Assert.Equal(tone, agent.StatusTone);
    }
    [Fact]
    public async Task Trace_renders_uppercase_identity_developer_avatar_reason_and_friendly_status()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var run = new ChatRun("Verifique meus documentos", ["nota.xml", "livro.txt"]) { State = "parcial", ExecutionId = Guid.NewGuid().ToString() };
        run.Agents.Add(new("nfse-validador", "Agente Validador NFS-e", "concluida", 1250, Developer: "Marcos", SelectionReason: "Especialidade identificada no conteúdo dos documentos.", DocumentsSent: 1));
        run.Agents.Add(new("agente-validacao-ecd", "Agente Validação ECD", "falhou", 2800, Error: "AGENT_CONNECTION_FAILED", Developer: "Carol", SelectionReason: "Especialidade compatível com a pergunta e os documentos.", DocumentsSent: 1));
        run.Agents.Add(new("agente-parecer-tributario", "Agente Parecer Tributário", "concluida", 400, Developer: "Roberto Rabelo", Stage: "parecer_final"));
        run.Trace.Add(new(DateTimeOffset.UtcNow, "Especialistas selecionados", "AGENTE VALIDADOR NFS-E · AGENTE VALIDAÇÃO ECD"));
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<RunTrace>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Run"] = run }))).ToHtmlString());
        var decoded = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("AGENTE VALIDADOR NFS-E", decoded); Assert.Contains("AGENTE VALIDAÇÃO ECD", decoded);
        Assert.Contains("Desenvolvido por Marcos", decoded); Assert.Contains("Desenvolvido por Carol", decoded);
        Assert.Contains("specialist-avatar", html); Assert.Contains("Concluído", decoded); Assert.Contains("Falha", decoded);
        Assert.Contains("1 documento(s)", decoded); Assert.Contains("Especialidade identificada", decoded);
        Assert.Contains("Última etapa · parecer final", decoded);
        Assert.Contains("Consolidação de 2 especialistas", decoded);
        Assert.Contains("agent-color-0", html); Assert.Contains("agent-color-1", html); Assert.Contains("agent-color-7", html);
        Assert.DoesNotContain("em_andamento", html);
        Assert.DoesNotContain("specialist-trace\" open", html);
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var css = File.ReadAllText(Path.Combine(root, "SofiaInfis.Web/wwwroot/sofia.css")) + File.ReadAllText(Path.Combine(root, "SofiaInfis.Web/wwwroot/refinements.css"));
        Directory.CreateDirectory(Path.Combine(root, "artifacts/routing-preview"));
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts/routing-preview/index.html"),
            "<!doctype html><html lang='pt-BR'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>" + css +
            "body{margin:0;padding:24px;background:#f5f1ea}.preview-wrap{max-width:900px;margin:auto}</style><body><main class='preview-wrap'><h2>Rastreamento dos especialistas</h2>" + html + "</main></body></html>");
    }
}
