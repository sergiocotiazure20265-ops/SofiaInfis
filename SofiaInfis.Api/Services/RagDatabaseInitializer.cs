using Npgsql;

namespace SofiaInfis.Api.Services;

public sealed class RagDatabaseInitializer(IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Rag:InitializeDatabase")) return;
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("PostgreSQL"));
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(72501931);
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS conhecimentos (
                id uuid PRIMARY KEY,
                titulo text NOT NULL,
                texto text NOT NULL,
                fonte text NOT NULL,
                indice_chunk integer NOT NULL,
                embedding vector(1536) NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_conhecimentos_embedding
                ON conhecimentos USING hnsw (embedding vector_cosine_ops);
            """, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
