using System.Text;
using System.Text.Json;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Transport;
namespace SofiaInfis.Api.Services.A2A.Domain;

public sealed class OrchestrationInputValidator(A2AOptions options)
{
    public static bool Text(string? value, int min = 1, int max = int.MaxValue) =>
        !string.IsNullOrWhiteSpace(value) && value.EnumerateRunes().Count() >= min && value.EnumerateRunes().Count() <= max;
    public void Validate(CriarOrquestracaoRequest request)
    {
        if (request.usuario is null || !Text(request.usuario.nome?.Trim(), 1, 200) || !Text(request.pergunta, 5, 2000) ||
            request.documentos is null || request.documentos.Count is < 1 or > 5 ||
            request.capacidades is null || request.capacidades.Count == 0 ||
            request.capacidades.Any(x => !Text(x)) || request.capacidades.Distinct(StringComparer.Ordinal).Count() != request.capacidades.Count)
            throw new A2AException("validacao", "INVALID_ANALYSIS_REQUEST");
        if (request.documentos.Any(x => x is null || !Text(x.idDocumento) || !Text(x.nome) || !Text(x.texto, 1, 20000) ||
            !x.textoCompleto || (x.tipo is not null && !Text(x.tipo))) ||
            request.documentos.Select(x => x.idDocumento).Distinct(StringComparer.Ordinal).Count() != request.documentos.Count)
            throw new A2AException("validacao", "INVALID_DOCUMENT");
        var probe = DomainRequest(request, Guid.Empty, Guid.Empty);
        var wire = A2A03JsonCodec.MessageRequest(probe, "rpc-probe", Guid.Empty.ToString());
        if (Encoding.UTF8.GetByteCount(wire) > options.MaxEnvelopeBytes) throw new A2AException("validacao", "ENVELOPE_TOO_LARGE", 413);
    }
    public static SolicitacaoAnaliseSofia DomainRequest(CriarOrquestracaoRequest request, Guid execution, Guid call) =>
        new("1.0", execution, call, new(request.usuario.nome.Trim()), request.pergunta,
            request.documentos.Select(x => new DocumentoAnalise(x.idDocumento, x.nome, x.texto, x.tipo)).ToList());
}
