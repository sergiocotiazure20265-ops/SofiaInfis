using System.Net;
using System.Text.Json.Nodes;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Contract;

public sealed class AgentsEndpointContractTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Discovery_is_authenticated_and_output_does_not_expose_credential_references()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var configuration = new A2ATestConfiguration(mongo, agent);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/agentes")).StatusCode);
        using var client = configuration.Client(factory);
        HttpResponseMessage? response = null;
        for (var i = 0; i < 40; i++)
        {
            response?.Dispose();
            response = await client.GetAsync("/api/v1/agentes");
            if (response.StatusCode != HttpStatusCode.ServiceUnavailable) break;
            await Task.Delay(50);
        }
        using (response)
        {
            Assert.Equal(HttpStatusCode.OK, response!.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(agent.Token, text);
            Assert.DoesNotContain("credentialRef", text, StringComparison.OrdinalIgnoreCase);
            var json = JsonNode.Parse(text)!;
            Assert.True(json["items"]![0]!["elegivel"]!.GetValue<bool>());
        }
    }
}
