using Dapper;
using Npgsql;
using SofiaInfis.Api.DTOs;
using System.Globalization;

namespace SofiaInfis.Api.Services
{
    public class ConhecimentoService
    {
        private readonly string _connectionString;
        private readonly EmbeddingService _embeddingService;

        public ConhecimentoService(IConfiguration configuration, EmbeddingService embeddingService)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:PostgreSQL não configurada.");

            _embeddingService = embeddingService;
        }

        // Divide o texto em blocos para geração de embeddings
        private static List<string> DividirTexto(string texto, int tamanho = 1200, int sobreposicao = 200)
        {
            if (tamanho <= 0 ||
                sobreposicao < 0 ||
                sobreposicao >= tamanho)
            {
                throw new ArgumentException(
                    "Parâmetros de chunking inválidos.");
            }

            var chunks = new List<string>();

            texto = texto.Trim();

            int inicio = 0;

            while (inicio < texto.Length)
            {
                int comprimento = Math.Min(
                    tamanho,
                    texto.Length - inicio);

                string chunk = texto
                    .Substring(inicio, comprimento)
                    .Trim();

                if (!string.IsNullOrWhiteSpace(chunk))
                    chunks.Add(chunk);

                if (inicio + comprimento >= texto.Length)
                    break;

                inicio += tamanho - sobreposicao;
            }

            return chunks;
        }

        // Converte float[] para o formato textual do PgVector
        private static string ConverterVetor(float[] vetor)
        {
            return "[" + string.Join(
                ",",
                vetor.Select(v =>
                    v.ToString(
                        "R",
                        CultureInfo.InvariantCulture))) + "]";
        }

        // Realiza a ingestão dos conhecimentos
        public async Task<object> IngestAsync(IngestConhecimentoRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.texto) ||
                string.IsNullOrWhiteSpace(request.titulo) ||
                string.IsNullOrWhiteSpace(request.fonte))
            {
                throw new ArgumentException(
                    "Título, texto e fonte são obrigatórios.");
            }

            var chunks = DividirTexto(request.texto);

            // Gera os embeddings antes de iniciar a transação
            var registros = new List<(string Texto, string Vetor)>();

            foreach (var chunk in chunks)
            {
                var embedding =
                    await _embeddingService.GerarEmbeddingAsync(
                        chunk,
                        cancellationToken);

                if (embedding.Length != 1536)
                    throw new InvalidOperationException(
                        "O embedding deve possuir 1536 dimensões.");

                registros.Add((
                    chunk,
                    ConverterVetor(embedding)
                ));
            }

            await using var conexao =
                new NpgsqlConnection(_connectionString);

            await conexao.OpenAsync(cancellationToken);

            await using var transacao =
                await conexao.BeginTransactionAsync(cancellationToken);

            const string sql = """
                INSERT INTO conhecimentos
                (
                    id,
                    titulo,
                    texto,
                    fonte,
                    indice_chunk,
                    embedding
                )
                VALUES
                (
                    @Id,
                    @Titulo,
                    @Texto,
                    @Fonte,
                    @IndiceChunk,
                    CAST(@Embedding AS vector)
                );
            """;

            var documentoId = Guid.NewGuid();

            try
            {
                for (int i = 0; i < registros.Count; i++)
                {
                    await conexao.ExecuteAsync(
                        new CommandDefinition(
                            sql,
                            new
                            {
                                Id = i == 0
                                    ? documentoId
                                    : Guid.NewGuid(),

                                request.titulo,
                                Texto = registros[i].Texto,
                                request.fonte,
                                IndiceChunk = i,
                                Embedding = registros[i].Vetor
                            },
                            transaction: transacao,
                            cancellationToken: cancellationToken
                        ));
                }

                await transacao.CommitAsync(cancellationToken);
            }
            catch
            {
                await transacao.RollbackAsync(CancellationToken.None);
                throw;
            }

            return new
            {
                Mensagem = "Conhecimento registrado com sucesso.",
                DocumentoReferencia = documentoId,
                Titulo = request.titulo,
                Fonte = request.fonte,
                TotalChunks = registros.Count
            };
        }

        // Busca conhecimentos por similaridade semântica
        public async Task<IEnumerable<ConhecimentoResultado>> BuscarAsync(
            BuscarConhecimentoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Pergunta))
                throw new ArgumentException(
                    "Informe uma pergunta.");

            if (request.TopK < 1 || request.TopK > 20)
                throw new ArgumentOutOfRangeException(
                    nameof(request.TopK));

            // Gera o embedding da pergunta
            var embedding =
                await _embeddingService.GerarEmbeddingAsync(
                    request.Pergunta,
                    cancellationToken);

            if (embedding.Length != 1536)
                throw new InvalidOperationException(
                    "Dimensão de embedding incompatível.");

            string vetor = ConverterVetor(embedding);

            const string sql = """
                SELECT
                    id AS Id,
                    titulo AS Titulo,
                    texto AS Texto,
                    fonte AS Fonte,
                    indice_chunk AS IndiceChunk,
                    (
                        1 - (
                            embedding <=> CAST(@Embedding AS vector)
                        )
                    )::double precision AS Similaridade
                FROM conhecimentos
                ORDER BY embedding <=> CAST(@Embedding AS vector)
                LIMIT @TopK;
            """;

            await using var conexao =
                new NpgsqlConnection(_connectionString);

            await conexao.OpenAsync(cancellationToken);

            var resultados =
                await conexao.QueryAsync<ConhecimentoResultado>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            Embedding = vetor,
                            request.TopK
                        },
                        cancellationToken: cancellationToken
                    ));

            return resultados;
        }
    }
}
