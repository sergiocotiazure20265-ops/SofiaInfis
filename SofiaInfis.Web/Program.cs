using SofiaInfis.Web.Components;
using SofiaInfis.Web.Services;
using SofiaInfis.Api.Services;
using SofiaInfis.Api.Extensions;
using SofiaInfis.Api.Controllers;

namespace SofiaInfis.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.Configure<SofiaOptions>(builder.Configuration.GetSection("Sofia"));
        builder.Services.AddOptions<SofiaOptions>().Configure<IConfiguration>((options, config) =>
            options.ApiAccessKey ??= config["Deployment:AccessKey"]);
        builder.Services.AddScoped<ExtracaoDocumentoService>();
        builder.Services.AddScoped<DocumentPreparation>();
        builder.Services.AddHttpClient<LiveSofiaChat>(client => client.Timeout = TimeSpan.FromSeconds(130))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, ConnectTimeout = TimeSpan.FromSeconds(5) });
        builder.Services.AddScoped<ISofiaChat>(services =>
            builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Sofia:DemoMode")
                ? new DemoSofiaChat() : services.GetRequiredService<LiveSofiaChat>());

        var embeddedApi = builder.Configuration.GetValue<bool>("Sofia:EmbeddedApi");
        if (embeddedApi)
        {
            builder.Services.AddControllers().AddApplicationPart(typeof(SofiaController).Assembly);
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerConfig();
            builder.Services.AddOpenAiClientConfig(builder.Configuration);
            builder.Services.AddDependencyInjectionConfig();
            builder.Services.AddA2AIntegration();
            builder.Services.AddHostedService<RagDatabaseInitializer>();
            builder.Services.AddHealthChecks().AddCheck<RagHealthCheck>("rag");
        }

        var app = builder.Build();
        app.UseDeploymentAccess(includeWeb: true);
        if (embeddedApi)
        {
            app.UseSwaggerConfig();
            app.MapOpenApi();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");
        }
        else app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
