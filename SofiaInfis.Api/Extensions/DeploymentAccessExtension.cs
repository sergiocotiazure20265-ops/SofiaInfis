using System.Security.Cryptography;
using System.Text;

namespace SofiaInfis.Api.Extensions;

public static class DeploymentAccessExtension
{
    public static IApplicationBuilder UseDeploymentAccess(this IApplicationBuilder app, bool includeWeb = false)
        => app.Use(async (context, next) =>
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var accessKey = config["Deployment:AccessKey"];
            if (context.Request.Path == "/health") { await next(context); return; }
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                if (config.GetValue<bool>("Deployment:RequireAccessKey"))
                { context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable; return; }
                await next(context); return;
            }
            var api = context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/swagger")
                || context.Request.Path.StartsWithSegments("/openapi");
            var permitted = api && Matches(context.Request.Headers["X-Sofia-Access-Key"].ToString(), accessKey);
            if (!api && !includeWeb) { await next(context); return; }
            if (!permitted && includeWeb)
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    try { permitted = Matches(Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..])), "sofia:" + accessKey); }
                    catch (FormatException) { }
                }
            }
            if (!permitted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.CacheControl = "no-store";
                if (includeWeb) context.Response.Headers.WWWAuthenticate = "Basic realm=\"SOFIA\", charset=\"UTF-8\"";
                return;
            }
            await next(context);
        });

    private static bool Matches(string supplied, string expected)
        => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
