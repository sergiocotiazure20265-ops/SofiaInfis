using System.Globalization;
using System.Text;
using SofiaInfis.Api.DTOs.A2A;

namespace SofiaInfis.Api.Services.A2A.Domain;

public sealed record AgentRoute(AgentDescriptor Agent, List<DocumentoEntrada> Documents, string Reason);

/// <summary>Local reviewed domain profiles: generic capabilities or MIME types never imply relevance.</summary>
public static class AgentRequestRouter
{
    public const string QuestionContextId = "sofia-question-context";

    public static AgentRoute[] Select(IEnumerable<AgentDescriptor> candidates, CriarOrquestracaoRequest request)
    {
        var question = Normalize(request.pergunta);
        return candidates.Select(agent =>
        {
            var profile = agent.Configuration;
            var byQuestion = profile.RoutingTerms.Any(term => Contains(question, term));
            var documents = request.documentos.Where(doc => doc.idDocumento != QuestionContextId &&
                profile.DocumentTerms.Any(term => Contains(Normalize(doc.nome + "\n" + doc.texto), term))).ToList();
            if (!byQuestion && documents.Count == 0) return null;
            var count = documents.Count;
            if (count == 0)
                documents.Add(new(QuestionContextId, "contexto-pergunta.txt", request.pergunta, "text/plain"));
            var reason = byQuestion && count > 0 ? "Especialidade compatível com a pergunta e os documentos." :
                count > 0 ? "Especialidade identificada no conteúdo dos documentos." :
                "Especialidade identificada na pergunta; enviado somente o contexto textual da pergunta.";
            return new AgentRoute(agent, documents, reason);
        }).OfType<AgentRoute>().ToArray();
    }

    private static bool Contains(string normalized, string term)
    {
        var needle = Normalize(term).Trim();
        return needle.Length > 0 && normalized.Contains(" " + needle + " ", StringComparison.Ordinal);
    }

    private static string Normalize(string text)
    {
        var result = new StringBuilder(" ");
        foreach (var rune in text.Normalize(NormalizationForm.FormD).EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark) continue;
            if (Rune.IsLetterOrDigit(rune)) result.Append(Rune.ToLowerInvariant(rune));
            else if (result[^1] != ' ') result.Append(' ');
        }
        if (result[^1] != ' ') result.Append(' ');
        return result.ToString();
    }
}
