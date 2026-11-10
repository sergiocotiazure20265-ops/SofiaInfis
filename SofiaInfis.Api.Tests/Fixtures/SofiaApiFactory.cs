using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace SofiaInfis.Api.Tests.Fixtures;

internal sealed class SofiaApiFactory(Dictionary<string, string?>? settings = null,
    Action<IServiceCollection>? configure = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SofiaInfis.Api")));
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-only-not-a-real-credential",
                ["OpenAI:ChatModel"] = "test-model",
                ["OpenAI:EmbeddingModel"] = "test-embedding",
                ["A2A:Enabled"] = "false"
            }.Concat(settings ?? []).GroupBy(k => k.Key).ToDictionary(k => k.Key, v => v.Last().Value)));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ChatClient>();
            services.RemoveAll<EmbeddingClient>();
            var options = new OpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(new HttpClient(new SyntheticOpenAIHandler()))
            };
            services.AddSingleton(new ChatClient("test", new ApiKeyCredential("test-only"), options));
            services.AddSingleton(new EmbeddingClient("test", new ApiKeyCredential("test-only"), options));
            configure?.Invoke(services);
        });
    }
}
