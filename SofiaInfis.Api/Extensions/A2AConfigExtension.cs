using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Services.A2A.Security;
using SofiaInfis.Api.Services.A2A.Transport;
using SofiaInfis.Api.Services.A2A.Workers;
using SofiaInfis.Api.Services.A2A;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SofiaInfis.Api.Extensions;
public static class A2AConfigExtension
{
    public static IServiceCollection AddA2AIntegration(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var options = new A2AOptions();
            try { configuration.GetSection("A2A").Bind(options); }
            catch (InvalidOperationException) { options.Enabled = true; options.MaxEnvelopeBytes = 0; }
            options.MongoConnectionString ??= configuration.GetConnectionString("MongoDB");
            options.MongoDatabase ??= configuration["MongoDB:DatabaseName"];
            return options;
        });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<A2AModuleAvailability>();
        services.AddSingleton<IMongoClient>(provider => MongoExecutionRepository.CreateClient(provider.GetRequiredService<A2AOptions>()));
        services.AddScoped<IExecutionRepository, MongoExecutionRepository>();
        services.AddSingleton<IAgentRegistry, ConfiguredAgentRegistry>();
        services.AddScoped<IAgentDiscovery, AgentDiscoveryService>();
        services.AddScoped<NoEligibleAgentExecutionRecorder>();
        services.AddScoped<OrchestrationInputValidator>();
        services.AddScoped<ISofiaAnalysisValidator, SofiaAnalysisValidator>();
        services.AddScoped<IA2AAgentClient, JsonRpc03AgentClient>();
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddSingleton<OrchestrationQueue>();
        services.AddSingleton<ExecutionCoordinator>();
        services.AddDataProtection();
        services.AddSingleton<ExecutionCursorProtector>();
        services.AddScoped<ExecutionRecoveryWorker>();
        services.AddHostedService<ExecutionRetentionWorker>();
        services.AddSingleton<A2AObservability>();
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource("SofiaInfis.A2A").AddConsoleExporter())
            .WithMetrics(metrics => metrics.AddMeter("SofiaInfis.A2A").AddConsoleExporter());
        services.AddScoped<AgentExecutionPipeline>();
        services.AddScoped<SingleAgentExecutionPipeline>();
        services.AddHostedService<OrchestrationWorker>();
        services.AddSingleton<AgentCredentialProvider>();
        services.AddHttpClient();
        services.AddSingleton<IConfigureOptions<HttpClientFactoryOptions>, A2AHttpClientConfiguration>();
        services.AddTransient<A2AHttpClientFactory>();
        services.AddScoped<A2AEnabledResourceFilter>();
        services.AddScoped<A2AExceptionFilter>();
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(behavior =>
        {
            var original = behavior.InvalidModelStateResponseFactory;
            behavior.InvalidModelStateResponseFactory = context =>
                context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor action &&
                (action.ControllerTypeInfo.AsType() == typeof(Controllers.OrquestracoesController) ||
                 action.ControllerTypeInfo.AsType() == typeof(Controllers.AgentesController))
                ? A2AProblemFactory.Create(context.HttpContext, 400, "INVALID_ANALYSIS_REQUEST")
                : original(context);
        });
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<A2AOptions>((cors, settings) =>
        {
            var origins = settings.AllowedBrowserOrigins.Where(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
                string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) &&
                string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath == "/" && !origin.EndsWith('/')).ToArray();
            cors.AddPolicy("SofiaBrowser", policy => policy.WithOrigins(origins).WithMethods("GET", "POST", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization"));
        });
        services.AddHostedService<A2AModuleInitializer>();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, SofiaA2AServiceHandler>(
            SofiaA2AServiceHandler.SchemeName, _ => { });
        services.AddAuthorization(options =>
        {
            foreach (var permission in A2AAuthorizationPolicies.Permissions)
                options.AddPolicy(permission, policy => policy.AddAuthenticationSchemes(SofiaA2AServiceHandler.SchemeName)
                    .RequireAuthenticatedUser().RequireClaim("permission", permission));
        });
        return services;
    }
}
