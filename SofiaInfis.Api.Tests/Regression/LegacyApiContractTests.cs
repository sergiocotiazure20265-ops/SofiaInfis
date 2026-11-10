using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Regression;

public sealed class LegacyApiContractTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("false", "1048576")]
    [InlineData("true", "1048576")]
    [InlineData("true", "0")]
    public async Task Existing_routes_and_full_flows_work_without_new_credentials(string enabled, string limit)
    {
        await using var factory = new SofiaApiFactory(new()
        {
            ["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString, ["A2A:Enabled"] = enabled,
            ["A2A:MaxEnvelopeBytes"] = limit,
            ["A2A:MongoConnectionString"] = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=100",
            ["A2A:MongoDatabase"] = "sofia_a2a_tests_unavailable"
        });
        using var client = factory.CreateClient();
        var swagger = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        foreach (var path in new[] { "/api/v1/sofia/perguntar", "/api/v1/conhecimentos/ingest", "/api/v1/conhecimentos/buscar" })
            Assert.NotNull(swagger["paths"]![path]!["post"]);
        using var ingest = await client.PostAsJsonAsync("/api/v1/conhecimentos/ingest", new
        { titulo = "Conhecimento sintético", texto = "Documento sintético para validar preservação de ingestão e RAG.", fonte = "teste-local" });
        Assert.Equal(HttpStatusCode.OK, ingest.StatusCode);
        var ingestion = JsonNode.Parse(await ingest.Content.ReadAsStringAsync())!;
        Assert.Equal(1, ingestion["totalChunks"]!.GetValue<int>());
        using var search = await client.PostAsJsonAsync("/api/v1/conhecimentos/buscar", new { pergunta = "Verifique o documento", topK = 1 });
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var results = JsonNode.Parse(await search.Content.ReadAsStringAsync())!.AsArray();
        Assert.Single(results);
        Assert.Equal(1, results[0]!["similaridade"]!.GetValue<double>(), 5);
        using var form = new MultipartFormDataContent { { new StringContent("Analise a tributação sintética"), "pergunta" } };
        if (enabled == "true") form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("Documento TXT sintético")), "arquivos", "sintetico.txt");
        using var response = await client.PostAsync("/api/v1/sofia/perguntar", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var answer = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal(new[] { "arquivosProcessados", "conhecimentosUtilizados", "duracaoMs", "idExecucao", "pergunta", "resposta", "tokensEntrada", "tokensSaida", "tokensTotal" }, answer.Select(x => x.Key).Order().ToArray());
        Assert.True(Guid.TryParse(answer["idExecucao"]!.GetValue<string>(), out _));
        Assert.Equal(2, answer["tokensTotal"]!.GetValue<int>());
        Assert.Equal(enabled == "true" ? 1 : 0, answer["arquivosProcessados"]!.AsArray().Count);
        Assert.NotEmpty(answer["conhecimentosUtilizados"]!.AsArray());
    }
}
