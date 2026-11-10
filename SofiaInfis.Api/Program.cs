using SofiaInfis.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerConfig();
builder.Services.AddOpenAiClientConfig(builder.Configuration);
builder.Services.AddDependencyInjectionConfig();
builder.Services.AddA2AIntegration();
builder.Services.AddHostedService<SofiaInfis.Api.Services.RagDatabaseInitializer>();
builder.Services.AddHealthChecks().AddCheck<SofiaInfis.Api.Services.RagHealthCheck>("rag");

var app = builder.Build();

app.UseDeploymentAccess();
app.MapOpenApi();
app.UseSwaggerConfig();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
