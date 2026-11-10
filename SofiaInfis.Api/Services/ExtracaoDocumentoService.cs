using System.Text;
using System.Xml;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SofiaInfis.Api.Services
{
    public class ExtracaoDocumentoService
    {
        private const long TamanhoMaximoArquivo = 5 * 1024 * 1024;
        private const int MaximoCaracteresPorArquivo = 20000;

        public async Task<string> ExtrairTextoAsync(IFormFile arquivo, CancellationToken cancellationToken = default)
        {
            if (arquivo.Length == 0)
                throw new ArgumentException("O arquivo está vazio.");

            if (arquivo.Length > TamanhoMaximoArquivo)
                throw new ArgumentException("O arquivo não pode ultrapassar 5 MB.");

            string extensao = Path.GetExtension(
                arquivo.FileName).ToLowerInvariant();

            if (extensao is not (".pdf" or ".txt" or ".xml"))
                throw new ArgumentException("Somente arquivos PDF, TXT e XML são permitidos.");

            await using var stream = arquivo.OpenReadStream();

            string texto;

            if (extensao == ".pdf")
            {
                texto = ExtrairPdf(stream);
            }
            else if (extensao == ".xml")
            {
                try
                {
                    using var reader = XmlReader.Create(stream, new XmlReaderSettings
                    {
                        Async = true, DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null, MaxCharactersInDocument = TamanhoMaximoArquivo
                    });
                    var xml = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
                    if (xml.Root is null) throw new InvalidDataException("XML sem elemento raiz.");
                    // Preserve field names and attributes for fiscal documents such as NF-e.
                    texto = xml.Root.ToString();
                }
                catch (XmlException ex) { throw new InvalidDataException("XML inválido ou com DTD proibido.", ex); }
            }
            else
            {
                using var reader = new StreamReader(
                    stream,
                    new UTF8Encoding(false, true),
                    detectEncodingFromByteOrderMarks: true);

                texto = await reader.ReadToEndAsync(
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Não foi possível extrair texto do arquivo.");

            // Limita o conteúdo enviado à OpenAI
            if (texto.Length > MaximoCaracteresPorArquivo)
            {
                texto = texto[..MaximoCaracteresPorArquivo]
                    + "\n[DOCUMENTO TRUNCADO]";
            }

            return texto;
        }

        private static string ExtrairPdf(Stream stream)
        {
            var texto = new StringBuilder();

            using var documento = PdfDocument.Open(stream);

            foreach (Page pagina in documento.GetPages())
            {
                texto.AppendLine(pagina.Text);
                texto.AppendLine();
            }

            return texto.ToString();
        }
    }
}
