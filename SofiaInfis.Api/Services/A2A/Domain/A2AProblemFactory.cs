using Microsoft.AspNetCore.Mvc;
namespace SofiaInfis.Api.Services.A2A.Domain;
public static class A2AProblemFactory
{
    public static ObjectResult Create(HttpContext context, int status, string code, string? executionId = null)
    {
        var problem = new ProblemDetails { Status = status, Title = "Não foi possível concluir a operação A2A." };
        problem.Extensions["codigo"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (executionId is not null) problem.Extensions["idExecucao"] = executionId;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
