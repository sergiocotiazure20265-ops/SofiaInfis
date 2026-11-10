using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using SofiaInfis.Api.Tests.Fixtures;

namespace SofiaInfis.Api.Tests.Web;

public sealed class CloudDeploymentTests
{
    [Fact]
    public async Task Api_requires_deployment_key_and_fails_closed_when_required_key_is_missing()
    {
        await using var api = new SofiaApiFactory(new() { ["Deployment:AccessKey"] = "synthetic-secret" });
        using var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Sofia-Access-Key", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Sofia-Access-Key");
        client.DefaultRequestHeaders.Add("X-Sofia-Access-Key", "synthetic-secret");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        await using var missing = new SofiaApiFactory(new() { ["Deployment:RequireAccessKey"] = "true", ["Deployment:AccessKey"] = "" });
        using var closed = missing.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await closed.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task Web_challenges_anonymous_users_and_does_not_expose_key_in_html()
    {
        await using var web = new WebApplicationFactory<SofiaInfis.Web.Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Deployment:AccessKey"] = "synthetic-cloud-password" })));
        using var client = web.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var anonymous = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Contains(anonymous.Headers.WwwAuthenticate, h => h.Scheme == "Basic");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("sofia:synthetic-cloud-password")));
        var html = await client.GetStringAsync("/");
        Assert.Contains("SOFIA", html);
        Assert.DoesNotContain("synthetic-cloud-password", html);
    }
}
