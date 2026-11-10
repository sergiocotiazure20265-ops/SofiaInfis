using Microsoft.AspNetCore.Http;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services;
namespace SofiaInfis.Web.Services;

public sealed class DocumentPreparation(ExtracaoDocumentoService extraction)
{
    public const int MaxFiles = 5;
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const long MaxTotalBytes = 20 * 1024 * 1024;
    public static void Validate(IReadOnlyList<ChatFile> files)
    {
        if (files.Count > MaxFiles) throw new SofiaUiException("Adicione até 5 documentos por mensagem.");
        if (files.Sum(f => (long)f.Content.Length) > MaxTotalBytes) throw new SofiaUiException("Os documentos devem somar até 20 MB.");
        foreach (var file in files)
        {
            if (file.Content.Length == 0) throw new SofiaUiException($"O arquivo {file.Name} está vazio.");
            if (file.Content.Length > MaxFileBytes) throw new SofiaUiException("Cada documento deve ter até 5 MB.");
            if (Path.GetExtension(file.Name).ToLowerInvariant() is not (".pdf" or ".txt" or ".xml"))
                throw new SofiaUiException("Use documentos PDF com texto selecionável ou arquivos TXT e XML.");
        }
    }
    public async Task<List<DocumentoEntrada>> PrepareAsync(IReadOnlyList<ChatFile> files, CancellationToken ct)
    {
        Validate(files);
        var result = new List<DocumentoEntrada>();
        foreach (var file in files)
        {
            using var stream = new MemoryStream(file.Content, writable: false);
            var form = new FormFile(stream, 0, file.Content.Length, "arquivos", file.Name);
            string text;
            try { text = await extraction.ExtrairTextoAsync(form, ct); }
            catch (OperationCanceledException) { throw; }
            catch { throw new SofiaUiException($"Não consegui ler {file.Name}. Verifique se o documento contém texto e não está protegido."); }
            if (text.Length > 20000)
                throw new SofiaUiException($"O texto de {file.Name} ultrapassa 20.000 caracteres. Divida o documento para enviar o conteúdo completo.");
            result.Add(new("doc-" + Guid.NewGuid().ToString("N"), file.Name, text,
                Path.GetExtension(file.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : Path.GetExtension(file.Name).Equals(".xml", StringComparison.OrdinalIgnoreCase) ? "application/xml" : "text/plain"));
        }
        return result;
    }
}
