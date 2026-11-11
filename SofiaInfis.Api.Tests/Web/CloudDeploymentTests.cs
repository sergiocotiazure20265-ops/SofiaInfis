using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
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

    [Theory]
    [InlineData("synthetic-cloud-password")]
    [InlineData("")]
    public async Task Web_is_public_without_browser_challenge_and_does_not_expose_key(string accessKey)
    {
        await using var web = new WebApplicationFactory<SofiaInfis.Web.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider());
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deployment:AccessKey"] = accessKey,
                ["Deployment:RequireAccessKey"] = "true",
                ["Sofia:EmbeddedApi"] = "false"
            }));
        });
        using var client = web.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var anonymous = await client.GetAsync("/");
        var html = await anonymous.Content.ReadAsStringAsync();
        Assert.True(anonymous.StatusCode == HttpStatusCode.OK, html);
        Assert.Empty(anonymous.Headers.WwwAuthenticate);
        Assert.Contains("SOFIA", html);
        Assert.DoesNotContain("synthetic-cloud-password", html);
        foreach (var path in new[] { "/conhecimentos", "/chat.js", "/voice-input.mjs", "/_framework/blazor.web.js" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers.WwwAuthenticate);
        }
        var negotiate = await client.PostAsync("/_blazor/negotiate?negotiateVersion=1", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, negotiate.StatusCode);
        Assert.Empty(negotiate.Headers.WwwAuthenticate);
        var protectedApi = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(string.IsNullOrEmpty(accessKey) ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Unauthorized, protectedApi.StatusCode);
        Assert.Empty(protectedApi.Headers.WwwAuthenticate);
    }
}
