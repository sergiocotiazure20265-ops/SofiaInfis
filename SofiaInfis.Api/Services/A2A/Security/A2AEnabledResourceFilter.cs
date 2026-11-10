using Microsoft.AspNetCore.Mvc.Filters;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Transport;
namespace SofiaInfis.Api.Services.A2A.Security;
public sealed class A2AEnabledResourceFilter(A2AModuleAvailability availability, A2AOptions options) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!availability.Enabled || !availability.Valid || !availability.Ready)
        {
            context.Result = A2AProblemFactory.Create(context.HttpContext, 503, "A2A_UNAVAILABLE");
            return;
        }
        var request = context.HttpContext.Request;
        if (HttpMethods.IsPost(request.Method) && !request.Path.Value!.EndsWith("/cancelar", StringComparison.Ordinal))
        {
            try
            {
                var bytes = await JsonWire.ReadAsync(request.Body, options.MaxEnvelopeBytes, context.HttpContext.RequestAborted);
                using var document = JsonWire.Parse(bytes);
                using var body = new MemoryStream(bytes);
                request.Body = body;
                await next();
            }
            catch (A2AException ex) { context.Result = A2AProblemFactory.Create(context.HttpContext, ex.HttpStatus, ex.Code); }
            return;
        }
        await next();
    }
}
public sealed class A2AExceptionFilter(ILogger<A2AExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception as A2AException;
        var status = exception?.HttpStatus ?? 503;
        var code = A2ATelemetrySanitizer.Code(exception?.Code ?? "A2A_OPERATION_FAILED");
        logger.LogWarning("A2A operation failed: {Code}, trace {TraceId}", code, context.HttpContext.TraceIdentifier);
        context.Result = A2AProblemFactory.Create(context.HttpContext, status, code);
        context.ExceptionHandled = true;
    }
}
