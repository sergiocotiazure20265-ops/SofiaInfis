using SofiaInfis.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerConfig();
builder.Services.AddOpenAiClientConfig(builder.Configuration);
builder.Services.AddDependencyInjectionConfig();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerConfig();
app.UseAuthorization();
app.MapControllers();
app.Run();
