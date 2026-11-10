using MongoDB.Driver;

namespace SofiaInfis.Api.Tests.Fixtures;

public sealed class MongoFixture : IAsyncLifetime
{
    private MongoClient? client;
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("SOFIA_TEST_MONGO")
        ?? "mongodb://127.0.0.1:27029/?serverSelectionTimeoutMS=2000";
    public string DatabaseName { get; } = "sofia_a2a_tests_" + Guid.NewGuid().ToString("N");
    public IMongoDatabase Database => (client ??= new MongoClient(ConnectionString)).GetDatabase(DatabaseName);
    public async Task InitializeAsync() => await Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
        new MongoDB.Bson.BsonDocument("ping", 1));
    public async Task DisposeAsync()
    {
        if (!DatabaseName.StartsWith("sofia_a2a_tests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Only disposable test databases may be removed.");
        await (client ??= new MongoClient(ConnectionString)).DropDatabaseAsync(DatabaseName);
    }
}
