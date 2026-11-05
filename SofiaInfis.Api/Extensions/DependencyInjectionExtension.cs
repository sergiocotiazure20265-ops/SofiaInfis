using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddDependencyInjectionConfig(this IServiceCollection services)
        {
            services.AddScoped<ConhecimentoService>();
            services.AddScoped<EmbeddingService>();
            services.AddScoped<ExtracaoDocumentoService>();
            services.AddScoped<SofiaService>();

            return services;
        }
    }
}
