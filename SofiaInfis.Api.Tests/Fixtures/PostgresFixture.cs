using Npgsql;
namespace SofiaInfis.Api.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim Initialization = new(1);
    private readonly string schema = "sofia_test_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = "127.0.0.1", Port = 54329, Database = "postgres", Username = "postgres",
        Password = "sofia-local-test-only", SearchPath = schema + ",public", Timeout = 3
    }.ConnectionString;
    public async Task InitializeAsync()
    {
        await Initialization.WaitAsync();
        try
        {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE EXTENSION IF NOT EXISTS vector; CREATE SCHEMA {schema}; CREATE TABLE {schema}.conhecimentos (id uuid PRIMARY KEY, titulo text, texto text, fonte text, indice_chunk integer, embedding vector(1536));";
        await command.ExecuteNonQueryAsync();
        }
        finally { Initialization.Release(); }
    }
    public async Task DisposeAsync()
    {
        if (!schema.StartsWith("sofia_test_", StringComparison.Ordinal)) throw new InvalidOperationException();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }
}
