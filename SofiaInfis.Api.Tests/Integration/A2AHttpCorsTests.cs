using System.Net;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;
public sealed class A2AHttpCorsTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Browser_policy_is_explicit_but_backend_calls_do_not_depend_on_Origin()
    {
        using var configuration = new A2ATestConfiguration(mongo);
        configuration.Settings["A2A:AllowedBrowserOrigins:0"] = "https://frontend.example";
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var client = configuration.Client(factory);
        for (int i = 0; i < 60; i++)
        {
            using var response = await client.GetAsync("/api/v1/agentes");
            if (response.StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/agentes")).StatusCode);
        using var allowed = new HttpRequestMessage(HttpMethod.Options, "/api/v1/orquestracoes");
        allowed.Headers.Add("Origin", "https://frontend.example");
        allowed.Headers.Add("Access-Control-Request-Method", "POST");
        allowed.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        using var accepted = await client.SendAsync(allowed);
        Assert.True(accepted.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("https://frontend.example", Assert.Single(values));
        using var blocked = new HttpRequestMessage(HttpMethod.Options, "/api/v1/orquestracoes");
        blocked.Headers.Add("Origin", "https://unknown.example");
        blocked.Headers.Add("Access-Control-Request-Method", "POST");
        using var denied = await client.SendAsync(blocked);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
