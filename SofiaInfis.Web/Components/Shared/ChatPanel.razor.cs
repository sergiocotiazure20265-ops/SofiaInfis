using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SofiaInfis.Web.Services;
namespace SofiaInfis.Web.Components.Shared;

public partial class ChatPanel
{
    [Parameter] public string UserName { get; set; } = "";
    [Parameter] public EventCallback OnLeave { get; set; }
    private string FirstName => UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? UserName;
    private string Initial => System.Globalization.StringInfo.GetNextTextElement(UserName).ToUpperInvariant();
    private readonly List<ChatRun> _runs = [];
    private readonly List<ChatFile> _files = [];
    private IReadOnlyList<SofiaInfis.Api.DTOs.A2A.AgenteDisponivel> _catalog = [];
    private string _question = "";
    private string? _notice;
    private bool _busy, _readingFiles, _sidebarOpen, _disposed;
    private int _fileInputKey;
    private ElementReference _scrollArea, _composer, _questionElement;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource _operation = new();
    private IJSObjectReference? _module, _binding;
    private DotNetObjectReference<ChatPanel>? _self;
    protected override async Task OnInitializedAsync()
    {
        try { _catalog = await Sofia.GetAgentsAsync(_lifetime.Token); }
        catch (Exception ex) when (ex is SofiaUiException or OperationCanceledException or HttpRequestException) { }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./chat.js");
            _self = DotNetObjectReference.Create(this);
            _binding = await _module.InvokeAsync<IJSObjectReference>("bindComposer", _composer, _self);
        }
    }
    private async Task FilesSelected(InputFileChangeEventArgs args)
    {
        if (_busy || _readingFiles) return;
        _readingFiles = true;
        try
        {
            var selected = args.GetMultipleFiles(DocumentPreparation.MaxFiles);
            if (_files.Count + selected.Count > 5) throw new SofiaUiException("Adicione até 5 documentos por mensagem.");
            if (_files.Sum(f => (long)f.Content.Length) + selected.Sum(f => f.Size) > DocumentPreparation.MaxTotalBytes) throw new SofiaUiException("Os documentos devem somar até 20 MB.");
            var pending = new List<ChatFile>();
            foreach (var file in selected)
            {
                if (Path.GetExtension(file.Name).ToLowerInvariant() is not (".pdf" or ".txt" or ".xml")) throw new SofiaUiException("Selecione documentos PDF, TXT ou XML.");
                await using var input = file.OpenReadStream(DocumentPreparation.MaxFileBytes, _lifetime.Token);
                using var output = new MemoryStream(); await input.CopyToAsync(output, _lifetime.Token);
                pending.Add(new(Path.GetFileName(file.Name.Replace('\\', '/')), output.ToArray()));
            }
            DocumentPreparation.Validate(_files.Concat(pending).ToArray()); _files.AddRange(pending); _notice = null;
        }
        catch (OperationCanceledException) { }
        catch (SofiaUiException ex) { _notice = ex.Message; }
        catch { _notice = "Não consegui anexar os arquivos. Use até 5 documentos PDF/TXT/XML, com no máximo 5 MB cada."; }
        finally { _readingFiles = false; _fileInputKey++; }
    }
    [JSInvokable] public async Task SendFromKeyboard() { await Send(); }
    private async Task Send()
    {
        if (_busy || _readingFiles || (string.IsNullOrWhiteSpace(_question) && _files.Count == 0)) return;
        var question = string.IsNullOrWhiteSpace(_question) ? "Analise os documentos anexados e apresente os principais achados e limitações." : _question.Trim();
        if (question.EnumerateRunes().Count() < 5) { _notice = "Conte um pouco mais: sua pergunta precisa ter pelo menos 5 caracteres."; return; }
        if (_runs.Count >= 20) { _notice = "Inicie uma nova conversa para continuar. Esta conversa atingiu 20 mensagens."; return; }
        _busy = true; _notice = null; _operation.Dispose(); _operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var input = new ChatInput(UserName, question, _files.ToArray());
        var run = new ChatRun(input.Question, input.Files.Select(f => f.Name).ToArray());
        _runs.Add(run); _question = ""; _files.Clear();
        try { await Sofia.SubmitAsync(input, run, Changed, _operation.Token); }
        catch (SofiaUiException ex) { run.State = "falhou"; run.Error = ex.Message; }
        catch (OperationCanceledException) { run.State = "acompanhamento_encerrado"; }
        catch { run.State = "falhou"; run.Error = "Não foi possível concluir esta análise. Tente novamente em instantes."; }
        finally { _busy = false; await Changed(); }
    }
    private async Task Consolidate(ChatRun run)
    {
        if (_busy) return;
        _busy = true; _notice = null; _operation.Dispose(); _operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        try { await Sofia.ConsolidateAsync(run, Changed, _operation.Token); }
        catch (OperationCanceledException) { _notice = "A espera da síntese foi encerrada. As análises dos agentes continuam disponíveis."; }
        catch (SofiaUiException ex) { _notice = ex.Message; }
        catch { _notice = "Não consegui gerar a síntese agora. As análises individuais foram preservadas."; }
        finally { _busy = false; await Changed(); }
    }
    private Task Changed() => _disposed ? Task.CompletedTask : InvokeAsync(async () => { StateHasChanged(); if (_module is not null) await _module.InvokeVoidAsync("follow", _scrollArea); });
    private void Stop() => _operation.Cancel();
    private async Task Suggest(string question) { _question = question; await _questionElement.FocusAsync(); }
    private async Task ScrollTo(ChatRun run) { _sidebarOpen = false; if (_module is not null) await _module.InvokeVoidAsync("scrollToTurn", "run-" + run.LocalId); }
    private async Task CopyId(ChatRun run) { try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", run.ExecutionId); _notice = "Identificador copiado."; } catch { _notice = "Selecione o identificador no rastreamento para copiá-lo."; } }
    private async Task NewChat() { if (_busy) return; _runs.Clear(); _files.Clear(); _question = ""; _notice = null; _sidebarOpen = false; await _questionElement.FocusAsync(); }
    private async Task Leave() { if (!_busy) await OnLeave.InvokeAsync(); }
    private static string AgentStatus(AgentView agent) => agent.Error is not null ? "Indisponível" : agent.Result?.status switch { "concluido" => "Concluído", "parcial" => "Parcial", "erro" => "Com limitação", _ => agent.State switch { "timeout" => "Prazo encerrado", "cancelada" => "Cancelado", "interrompida" => "Interrompido", _ => "Analisando…" } };
    public async ValueTask DisposeAsync()
    {
        _disposed = true; _lifetime.Cancel(); _operation.Cancel(); _files.Clear();
        try { if (_binding is not null) { await _binding.InvokeVoidAsync("dispose"); await _binding.DisposeAsync(); } if (_module is not null) await _module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
        _self?.Dispose(); _lifetime.Dispose(); _operation.Dispose();
    }
}
