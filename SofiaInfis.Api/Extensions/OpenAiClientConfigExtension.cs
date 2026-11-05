using OpenAI.Chat;
using OpenAI.Embeddings;
using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Extensions
{
    public static class OpenAiClientConfigExtension
    {
        public static IServiceCollection AddOpenAiClientConfig(this IServiceCollection services, IConfiguration configuration)
        {
            // Obtém a chave de acesso da OpenAI.
            var apiKey = configuration["OpenAI:ApiKey"];

            // Obtém o modelo utilizado para gerar embeddings.
            var embeddingModel = configuration["OpenAI:EmbeddingModel"];

            // Obtém o modelo utilizado para gerar respostas.
            var chatModel = configuration["OpenAI:ChatModel"];

            services.AddSingleton<EmbeddingClient>(_ =>
                new EmbeddingClient(
                    model: embeddingModel,
                    apiKey: apiKey));

            services.AddSingleton<ChatClient>(_ =>
                new ChatClient(
                    model: chatModel,
                    apiKey: apiKey));

            return services;
        }
    }
}