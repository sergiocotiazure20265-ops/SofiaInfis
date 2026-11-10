using System.Net;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Regression;

public sealed class A2AModuleIsolationTests
{
    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public async Task Legacy_validation_remains_available_when_new_module_has_no_agents_or_database(string enabled)
    {
        await using var factory = new SofiaApiFactory(new()
        {
            ["A2A:Enabled"] = enabled, ["A2A:MongoConnectionString"] = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=100",
            ["A2A:MongoDatabase"] = "sofia_a2a_tests_unavailable"
        });
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/v1/sofia/perguntar", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
    }
}
