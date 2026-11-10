using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Tests.Unit;
public sealed class ExecutionResultReducerTests
{
    [Theory]
    [InlineData("concluido", "concluido", false, "concluida")]
    [InlineData("concluido", "erro", false, "parcial")]
    [InlineData("parcial", "erro", false, "parcial")]
    [InlineData("erro", "erro", false, "falhou")]
    [InlineData("concluido", "concluido", true, "parcial")]
    public void Original_business_outcomes_determine_execution_state(string first, string second, bool missing, string expected)
    {
        var calls = new[] { Call(first), Call(second) };
        Assert.Equal(expected, ExecutionResultReducer.Reduce(calls, missing));
    }
    [Fact] public void No_agents_is_failure() => Assert.Equal("falhou", ExecutionResultReducer.Reduce([], true));
    private static AgentCallDocument Call(string status) => new() { Estado = "concluida", ResultadoJson = "{\"status\":\"" + status + "\"}" };
}
