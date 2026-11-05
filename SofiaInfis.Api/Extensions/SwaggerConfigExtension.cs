using Microsoft.OpenApi;

namespace SofiaInfis.Api.Extensions
{
    public static class SwaggerConfigExtension
    {
        public static IServiceCollection AddSwaggerConfig(
            this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SOFIA INFIS - API",
                    Version = "v1",
                    Description = """
                        API do agente orquestrador SOFIA da INFIS.

                        Recepção de documentos, integração com agentes,
                        consultas à base de conhecimento e processamento
                        de solicitações fiscais.

                        Para acessar os endpoints protegidos, clique em
                        Authorize e informe somente o token JWT.
                        """,
                    Contact = new OpenApiContact
                    {
                        Name = "INFIS Consultoria",
                        Url = new Uri("https://infisconsultoria.com.br/")
                    }
                });

                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = """
                            Informe somente o token JWT, sem o prefixo Bearer.
                            O Swagger adicionará esse prefixo automaticamente.
                            """
                    });

                options.AddSecurityRequirement(document =>
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(
                            "Bearer", document)] = []
                    });
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerConfig(
            this IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "./v1/swagger.json",
                    "SOFIA INFIS - API v1");

                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Documentação - SOFIA INFIS";

                options.DisplayRequestDuration();

                options.DefaultModelsExpandDepth(-1);
            });

            return app;
        }
    }
}