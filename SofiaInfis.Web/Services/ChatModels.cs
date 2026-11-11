using SofiaInfis.Api.DTOs.A2A;
namespace SofiaInfis.Web.Services;

public sealed class SofiaOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5151/";
    public string? ServiceToken { get; set; }
    public string? ApiAccessKey { get; set; }
    public string[] Capabilities { get; set; } = ["analisar_documentos"];
    public bool DemoMode { get; set; }
    public bool UseSpecialistAgents { get; set; }
    public bool AllowInternalHttp { get; set; }
    public int CatalogTimeoutSeconds { get; set; } = 30;
}
public sealed record ChatFile(string Name, byte[] Content)
{
    public string SizeLabel => Content.Length < 1024 * 1024 ? $"{Math.Ceiling(Content.Length / 1024d):0} KB" : $"{Content.Length / (1024d * 1024):0.0} MB";
}
public sealed record ChatInput(string Name, string Question, IReadOnlyList<ChatFile> Files);
public sealed record TraceEntry(DateTimeOffset At, string Title, string? Detail = null);
public sealed record AgentView(string Id, string Name, string State, long? DurationMs,
    ResultadoAnaliseSofia? Result = null, string? Error = null,
    SofiaInfis.Api.Services.A2A.Persistence.ConsumoInformado? Consumption = null,
    string? Developer = null, string? SelectionReason = null, int? DocumentsSent = null)
{
    public string StatusLabel => State switch
    {
        "aguardando" => "Na fila", "em_andamento" => "Analisando", "concluida" => "Concluído",
        "falhou" => "Falha", "timeout" => "Tempo esgotado", "cancelada" => "Cancelado",
        "interrompida" => "Interrompido", _ => "Preparando"
    };
    public string StatusTone => Error is not null || Result?.status == "erro" ? "warning" :
        State == "concluida" ? "success" : State is "aguardando" or "em_andamento" ? "active" : "warning";
}
public sealed class ChatRun(string question, IReadOnlyList<string> files)
{
    public Guid LocalId { get; } = Guid.NewGuid();
    public string Question { get; } = question;
    public IReadOnlyList<string> Files { get; } = files;
    public string? ExecutionId { get; set; }
    public string State { get; set; } = "preparando";
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public string? Answer { get; set; }
    public string? Error { get; set; }
    public int? Tokens { get; set; }
    public string? SynthesisExecutionId { get; set; }
    public bool IsSynthesizing { get; set; }
    public List<TraceEntry> Trace { get; } = [];
    public List<AgentView> Agents { get; } = [];
    public bool HasUsableResults => Agents.Any(a => a.Result?.status is "concluido" or "parcial");
    public bool IsRunning => State is "preparando" or "em_andamento";
    public string StateLabel => State switch
    {
        "preparando" => "Preparando sua análise", "em_andamento" => "SOFIA analisando",
        "concluida" => "Análise concluída", "parcial" => "Análise parcialmente concluída",
        "cancelada" => "Execução cancelada", "interrompida" => "Execução interrompida",
        "acompanhamento_encerrado" => "Acompanhamento encerrado", _ => "Não foi possível concluir"
    };
}
public sealed class SofiaUiException(string message) : Exception(message);
public interface ISofiaChat
{
    bool IsDemo { get; }
    Task<IReadOnlyList<AgenteDisponivel>> GetAgentsAsync(CancellationToken ct);
    Task SubmitAsync(ChatInput input, ChatRun run, Func<Task> changed, CancellationToken ct);
    Task ConsolidateAsync(ChatRun run, Func<Task> changed, CancellationToken ct);
}
