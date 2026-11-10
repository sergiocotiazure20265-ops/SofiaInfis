using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Transport;
namespace SofiaInfis.Api.Tests.Contract;

public sealed class A2ACompatibilityMatrixTests
{
    [Theory]
    [InlineData("concluido")]
    [InlineData("parcial")]
    [InlineData("erro")]
    public void Optional_extensions_survive_round_trip_and_all_business_states_validate(string status)
    {
        var request = JsonSerializer.Deserialize<SolicitacaoAnaliseSofia>(File.ReadAllText(Path.Combine(SofiaDomainContractTests.Contracts, "examples/solicitacao-sofia.json")), JsonWire.Options)!;
        var result = JsonNode.Parse(File.ReadAllText(Path.Combine(SofiaDomainContractTests.Contracts, "examples/resultado-sofia.json")))!;
        result["status"] = status;
        if (status == "erro") result["erro"] = new JsonObject { ["codigo"] = "TEST", ["mensagem"] = "Synthetic error" };
        result["extensaoOpcional"] = new JsonObject { ["valor"] = 10 };
        var agent = new AgentDescriptor(new AgentOptions { IdAgente = result["agente"]!["idAgente"]!.GetValue<string>(), ExpectedName = result["agente"]!["nome"]!.GetValue<string>() }, new Uri("https://example.invalid/rpc"), []);
        var validator = new SofiaAnalysisValidator();
        var valid = validator.Validate(result.ToJsonString(), request, agent);
        var roundTrip = JsonNode.Parse(JsonSerializer.Serialize(valid, JsonWire.Options))!;
        Assert.Equal(10, roundTrip["extensaoOpcional"]!["valor"]!.GetValue<int>());
        foreach (var field in new[] { "idExecucao", "idSolicitacao" })
        {
            var unrelated = result.DeepClone(); unrelated[field] = Guid.NewGuid().ToString();
            Assert.Throws<A2AException>(() => validator.Validate(unrelated.ToJsonString(), request, agent));
        }
        var otherAgent = result.DeepClone(); otherAgent["agente"]!["idAgente"] = "unrelated-agent";
        Assert.Throws<A2AException>(() => validator.Validate(otherAgent.ToJsonString(), request, agent));
        var otherDocument = result.DeepClone();
        otherDocument["achados"] = new JsonArray(new JsonObject { ["idAchado"] = "a", ["descricao"] = "Synthetic finding", ["idDocumentos"] = new JsonArray("unrelated-document"), ["evidencias"] = new JsonArray() });
        Assert.Throws<A2AException>(() => validator.Validate(otherDocument.ToJsonString(), request, agent));
        foreach (var field in new[] { "versaoContrato", "idExecucao", "idSolicitacao", "agente", "status", "analise", "achados" })
        {
            var broken = result.DeepClone().AsObject(); broken.Remove(field);
            Assert.Throws<A2AException>(() => validator.Validate(broken.ToJsonString(), request, agent));
        }
        result["versaoContrato"] = "2.0";
        Assert.Throws<A2AException>(() => validator.Validate(result.ToJsonString(), request, agent));
    }
    [Theory]
    [InlineData("{\"jsonrpc\":\"1.0\",\"id\":\"a\",\"result\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"b\",\"result\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"a\",\"error\":{},\"result\":{}}")]
    public void Invalid_wire_envelopes_are_rejected(string json) => Assert.Throws<A2AException>(() => A2A03JsonCodec.ParseResult(Encoding.UTF8.GetBytes(json), "a"));
    [Fact]
    public void Escaped_wire_bytes_and_streaming_limits_are_enforced()
    {
        var request = new CriarOrquestracaoRequest(new("Pessoa"), "Pergunta sintética", Enumerable.Range(0, 5).Select(i => new DocumentoEntrada("doc" + i, "a.txt", string.Concat(Enumerable.Repeat("😀", 20000)))).ToList(), ["analisar_documentos"]);
        var exception = Assert.Throws<A2AException>(() => new OrchestrationInputValidator(new()).Validate(request));
        Assert.Equal("ENVELOPE_TOO_LARGE", exception.Failure.codigo);
    }
    [Fact]
    public async Task Response_byte_limit_is_checked_on_actual_stream()
    {
        using var stream = new MemoryStream(new byte[1025]);
        await Assert.ThrowsAsync<A2AException>(() => JsonWire.ReadAsync(stream, 1024, default));
    }
}
