using System.Security.Cryptography;
using System.Text;

namespace SofiaInfis.Api.Extensions;

public static class DeploymentAccessExtension
{
    public static IApplicationBuilder UseDeploymentAccess(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            var api = context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/swagger")
                || context.Request.Path.StartsWithSegments("/openapi");
            if (!api) { await next(context); return; }
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var accessKey = config["Deployment:AccessKey"];
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                if (config.GetValue<bool>("Deployment:RequireAccessKey"))
                { context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable; return; }
                await next(context); return;
            }
            var permitted = Matches(context.Request.Headers["X-Sofia-Access-Key"].ToString(), accessKey);
            if (!permitted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.CacheControl = "no-store";
                return;
            }
            await next(context);
        });

    private static bool Matches(string supplied, string expected)
        => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
