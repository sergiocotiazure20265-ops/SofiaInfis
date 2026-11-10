using System.Text;
using System.Text.Json;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Transport;
namespace SofiaInfis.Api.Services.A2A.Domain;

public sealed class SofiaAnalysisValidator : ISofiaAnalysisValidator
{
    public ResultadoAnaliseSofia Validate(string json, SolicitacaoAnaliseSofia request, AgentDescriptor agent)
    {
        using var document = JsonWire.Parse(Encoding.UTF8.GetBytes(json));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || new[] { "versaoContrato", "idExecucao", "idSolicitacao", "agente", "status", "analise", "achados" }
            .Any(x => !root.TryGetProperty(x, out var value) || value.ValueKind == JsonValueKind.Null))
            throw new A2AException("validacao", "INVALID_ANALYSIS_RESULT");
        ResultadoAnaliseSofia result;
        try { result = JsonSerializer.Deserialize<ResultadoAnaliseSofia>(json, JsonWire.Options)!; }
        catch (JsonException) { throw new A2AException("validacao", "INVALID_ANALYSIS_RESULT"); }
        if (result is null || result.versaoContrato != "1.0" || result.agente is null ||
            !OrchestrationInputValidator.Text(result.agente.nome) || !OrchestrationInputValidator.Text(result.analise) ||
            result.status is not ("concluido" or "parcial" or "erro") || result.achados is null ||
            (result.observacoes is not null && result.observacoes.Any(x => !OrchestrationInputValidator.Text(x))) ||
            (root.TryGetProperty("observacoes", out var observations) && observations.ValueKind != JsonValueKind.Array))
            throw new A2AException("validacao", "INVALID_ANALYSIS_RESULT");
        if (result.idExecucao != request.idExecucao || result.idSolicitacao != request.idSolicitacao ||
            result.agente.idAgente != agent.Configuration.IdAgente || result.agente.nome != agent.Configuration.ExpectedName)
            throw new A2AException("correlacao", "RESULT_CORRELATION_MISMATCH");
        var documents = request.documentos.Select(x => x.idDocumento).ToHashSet(StringComparer.Ordinal);
        foreach (var finding in result.achados)
        {
            if (finding is null || !OrchestrationInputValidator.Text(finding.idAchado) || !OrchestrationInputValidator.Text(finding.descricao) ||
                finding.idDocumentos is null || finding.idDocumentos.Count == 0 || finding.idDocumentos.Any(x => !documents.Contains(x)) ||
                finding.idDocumentos.Distinct(StringComparer.Ordinal).Count() != finding.idDocumentos.Count ||
                finding.evidencias is null || finding.evidencias.Any(x => !OrchestrationInputValidator.Text(x)) ||
                (finding.severidade is not null && finding.severidade is not ("baixa" or "media" or "alta" or "critica")))
                throw new A2AException("validacao", "INVALID_FINDING");
        }
        if ((result.status == "erro" && result.erro is null) ||
            (result.erro is not null && (!OrchestrationInputValidator.Text(result.erro.codigo) || !OrchestrationInputValidator.Text(result.erro.mensagem))))
            throw new A2AException("validacao", "INVALID_BUSINESS_ERROR");
        return result;
    }
}
