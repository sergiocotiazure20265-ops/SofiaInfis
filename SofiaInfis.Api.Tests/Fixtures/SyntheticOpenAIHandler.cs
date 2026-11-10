using System.Net;
using System.Text;
using System.Text.Json;

namespace SofiaInfis.Api.Tests.Fixtures;

internal sealed class SyntheticOpenAIHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object payload;
        if (request.RequestUri!.AbsolutePath.EndsWith("/embeddings", StringComparison.Ordinal))
        {
            var vector = new float[1536];
            vector[0] = 1;
            payload = new { @object = "list", model = "test", data = new[] { new { @object = "embedding", index = 0, embedding = vector } }, usage = new { prompt_tokens = 1, total_tokens = 1 } };
        }
        else
        {
            payload = new { id = "chatcmpl-synthetic", @object = "chat.completion", created = 1, model = "test", choices = new[] { new { index = 0, message = new { role = "assistant", content = "Resposta sintética sem uso de serviço externo." }, finish_reason = "stop" } }, usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 } };
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        });
    }
}
