using OpenAI.Embeddings;

namespace SofiaInfis.Api.Services
{
    public class EmbeddingService (EmbeddingClient _client)
    {        
        public async Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("O texto não pode estar vazio.");

            OpenAIEmbedding resultado =
                await _client.GenerateEmbeddingAsync(texto, cancellationToken: cancellationToken);

            return resultado.ToFloats().ToArray();
        }
    }
}
