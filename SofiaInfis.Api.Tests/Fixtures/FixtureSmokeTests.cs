using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace SofiaInfis.Api.Tests.Fixtures;

public sealed class FixtureSmokeTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _mongo;
    public FixtureSmokeTests(MongoFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task Local_agent_has_distinct_card_and_rpc_paths()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var client = new HttpClient();
        var card = JsonNode.Parse(await client.GetStringAsync(new Uri(agent.Origin, "/.well-known/agent-card.json")))!;
        Assert.Equal("/rpc", new Uri(card["url"]!.GetValue<string>()).AbsolutePath);
        Assert.Equal("0.3.0", card["protocolVersion"]!.GetValue<string>());
        Assert.Equal(0, agent.AnalysisCalls);
    }

    [Fact]
    public async Task Factory_never_calls_OpenAI()
    {
        await using var factory = new SofiaApiFactory();
        using var scope = factory.Services.CreateScope();
        var embedding = await scope.ServiceProvider.GetRequiredService<EmbeddingClient>().GenerateEmbeddingAsync("synthetic");
        Assert.Equal(1536, embedding.Value.ToFloats().Length);
        var result = await scope.ServiceProvider.GetRequiredService<ChatClient>().CompleteChatAsync([new UserChatMessage("synthetic")]);
        Assert.Contains("sintética", result.Value.Content[0].Text);
        Assert.StartsWith("sofia_a2a_tests_", _mongo.DatabaseName);
    }
}
