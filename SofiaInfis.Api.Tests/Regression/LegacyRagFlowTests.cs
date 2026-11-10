using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Services;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Regression;

public sealed class LegacyRagFlowTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Chunk_overlap_vector_search_and_transaction_rollback_are_preserved()
    {
        await using var factory = new SofiaApiFactory(new() { ["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString });
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ConhecimentoService>();
        var text = new string('A', 1000) + new string('B', 1000) + new string('C', 500);
        await service.IngestAsync(new IngestConhecimentoRequest("Teste de chunks", text, "fixture"));
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand("SELECT texto FROM conhecimentos ORDER BY indice_chunk", connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            var chunks = new List<string>();
            while (await reader.ReadAsync()) chunks.Add(reader.GetString(0));
            Assert.Equal(new[] { 1200, 1200, 500 }, chunks.Select(x => x.Length));
            Assert.Equal(chunks[0][1000..], chunks[1][..200]);
        }
        Assert.Equal(2, (await service.BuscarAsync(new BuscarConhecimentoRequest("Pergunta sintética", 2))).Count());
        await using (var command = new NpgsqlCommand("ALTER TABLE conhecimentos ADD CONSTRAINT reject_test CHECK (fonte <> 'rollback' OR indice_chunk = 0)", connection))
            await command.ExecuteNonQueryAsync();
        await Assert.ThrowsAsync<PostgresException>(() => service.IngestAsync(new IngestConhecimentoRequest("Rollback sintético", text, "rollback")));
        await using var count = new NpgsqlCommand("SELECT count(*) FROM conhecimentos WHERE fonte = 'rollback'", connection);
        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }
}
