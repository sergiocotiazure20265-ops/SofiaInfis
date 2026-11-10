using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
using SofiaInfis.Api.Tests.Integration;
namespace SofiaInfis.Api.Tests.Contract;
public sealed class ExecutionQueryContractTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Query_links_return_original_analysis_and_do_not_repeat_it()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var configuration = new A2ATestConfiguration(mongo, agent);
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        var id = await PartialOrchestrationTests.Start(client);
        using var scope = factory.Services.CreateScope();
        await PartialOrchestrationTests.Wait(scope.ServiceProvider.GetRequiredService<IExecutionRepository>(), id);
        using var response = await client.GetAsync("/api/v1/orquestracoes/" + id + "?pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal("concluida", json["estado"]!.GetValue<string>());
        var link = json["chamadas"]!["items"]![0]!["linkResultado"]!.GetValue<string>();
        var result = JsonNode.Parse(await client.GetStringAsync(link))!;
        Assert.Equal(id, result["idExecucao"]!.GetValue<string>());
        Assert.Equal(1, agent.AnalysisCalls);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/orquestracoes/" + id + "?pageSize=101")).StatusCode);
    }
}
