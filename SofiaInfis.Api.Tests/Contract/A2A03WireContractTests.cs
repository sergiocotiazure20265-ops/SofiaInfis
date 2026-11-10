using System.Text.Json.Nodes;
using SofiaInfis.Api.Tests.Contract;
namespace SofiaInfis.Api.Tests.Contract;

public sealed class A2A03WireContractTests
{
    [Theory]
    [InlineData("agent-card.json", "AgentCard")]
    [InlineData("a2a-message-send.json", "SendMessageRequest")]
    [InlineData("a2a-task-completed.json", "SendMessageResponse")]
    [InlineData("a2a-tasks-get.json", "GetTaskRequest")]
    [InlineData("a2a-tasks-cancel.json", "CancelTaskRequest")]
    public void Wire_fixtures_match_pinned_official_schema(string file, string definition)
    {
        var schema = SofiaDomainContractTests.Schema("a2a-official-v0.3.0.schema.json", "#/definitions/" + definition);
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(SofiaDomainContractTests.Contracts, "examples", file)));
        Assert.True(schema.Evaluate(node).IsValid);
    }
}
