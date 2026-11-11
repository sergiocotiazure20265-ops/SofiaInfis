using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SofiaInfis.Web.Components.Shared;
namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaWebRenderingTests
{
    [Fact]
    public async Task Landing_page_and_local_art_are_served_by_the_new_host()
    {
        await using var factory = new WebApplicationFactory<SofiaInfis.Web.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider());
        });
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("lang=\"pt-BR\"", html); Assert.Contains("Sua pr", html);
        Assert.Contains("sofia-welcome.png", html); Assert.DoesNotContain("test-service-token", html);
        Assert.True((await client.GetAsync("/images/sofia-welcome.png")).IsSuccessStatusCode);
        Assert.True((await client.GetAsync("/chat.js")).IsSuccessStatusCode);
        using var voiceModule = await client.GetAsync("/voice-input.mjs");
        Assert.True(voiceModule.IsSuccessStatusCode);
        Assert.Contains("javascript", voiceModule.Content.Headers.ContentType!.MediaType!);
    }
    [Fact]
    public async Task Untrusted_agent_markdown_is_encoded_while_supported_emphasis_is_rendered()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<MarkdownText>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Text"] = "**Resultado**\n<script>alert('test')</script>" }));
            return rendered.ToHtmlString();
        });
        Assert.Contains("<strong>Resultado</strong>", html);
        Assert.DoesNotContain("<script>", html); Assert.Contains("&lt;script&gt;", html);
    }
}
