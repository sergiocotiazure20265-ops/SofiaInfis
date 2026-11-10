namespace SofiaInfis.Api.Services.A2A.Domain;
public record A2AFailure(string categoria, string codigo, string mensagem);
public sealed class A2AException(string category, string code, int httpStatus = 400) : Exception(code)
{
    public string Category { get; } = category;
    public string Code { get; } = code;
    public int HttpStatus { get; } = httpStatus;
    public A2AFailure Failure => new(Category, Code, "A operação não pôde ser concluída.");
}
