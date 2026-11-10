using System.Net.Http.Json;
using Npgsql;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Tests.Fixtures;

namespace SofiaInfis.Api.Tests.Regression;

public sealed class RagBootstrapTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Startup_creates_missing_table_and_restart_preserves_knowledge()
    {
        // The fixture owns this isolated schema; never drop a development table.
        await using (var connection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("DROP TABLE conhecimentos", connection);
            await command.ExecuteNonQueryAsync();
        }
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString,
            ["Rag:InitializeDatabase"] = "true"
        };
        await using (var first = new SofiaApiFactory(settings))
        {
            using var client = first.CreateClient();
            Assert.True((await client.GetAsync("/health")).IsSuccessStatusCode);
            var response = await client.PostAsJsonAsync("/api/v1/conhecimentos/ingest", new IngestConhecimentoRequest(
                "Referência persistida", "Texto sintético preservado após reinicialização da API.", "Fixture isolada"));
            response.EnsureSuccessStatusCode();
        }
        await using var second = new SofiaApiFactory(settings);
        using var restarted = second.CreateClient();
        Assert.True((await restarted.GetAsync("/health")).IsSuccessStatusCode);
        var matches = await restarted.PostAsJsonAsync("/api/v1/conhecimentos/buscar", new BuscarConhecimentoRequest("Referência", 3));
        var rows = await matches.Content.ReadFromJsonAsync<ConhecimentoResultado[]>();
        Assert.Equal("Referência persistida", Assert.Single(rows!).titulo);
    }
}
