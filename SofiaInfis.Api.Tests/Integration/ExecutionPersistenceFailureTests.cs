using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Persistence;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class ExecutionPersistenceFailureTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Theory]
    [InlineData("insert")]
    [InlineData("before-result")]
    [InlineData("after-result")]
    public async Task Storage_failure_never_causes_analysis_resend_or_unconfirmed_result_claim(string failure)
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var config = new A2ATestConfiguration(mongo, agent);
        await using var factory = new SofiaApiFactory(config.Settings, services =>
        {
            services.RemoveAll<IExecutionRepository>();
            services.AddScoped<MongoExecutionRepository>();
            services.AddScoped<IExecutionRepository>(sp => new FaultRepository(sp.GetRequiredService<MongoExecutionRepository>(), failure));
        });
        using var client = config.Client(factory);
        for (var i = 0; i < 60; i++)
        {
            if ((await client.GetAsync("/api/v1/agentes")).StatusCode == HttpStatusCode.OK) break;
            await Task.Delay(50);
        }
        if (failure == "insert")
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request)).StatusCode);
            Assert.Equal(0, agent.AnalysisCalls);
            return;
        }
        var id = await PartialOrchestrationTests.Start(client);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<MongoExecutionRepository>();
        var execution = await PartialOrchestrationTests.Wait(repository, id);
        var call = Assert.Single(await repository.GetCallsAsync(id, default));
        Assert.Equal(1, agent.AnalysisCalls);
        Assert.NotNull(execution.FalhaRegistro);
        if (failure == "before-result")
        {
            Assert.Null(call.ResultadoJson);
            Assert.Equal("falhou", execution.Estado);
        }
        else
        {
            Assert.NotNull(call.ResultadoJson);
            Assert.Equal("concluida", execution.Estado);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/orquestracoes/{id}/chamadas/{call.Id}/resultado")).StatusCode);
        }
    }
    private sealed class FaultRepository(IExecutionRepository inner, string failure) : IExecutionRepository
    {
        public Task InitializeAsync(CancellationToken ct) => inner.InitializeAsync(ct);
        public Task InsertExecutionAsync(ExecutionDocument value, CancellationToken ct) => failure == "insert" ? Task.FromException(new IOException("Synthetic storage failure")) : inner.InsertExecutionAsync(value, ct);
        public Task<ExecutionDocument?> GetExecutionAsync(string id, CancellationToken ct) => inner.GetExecutionAsync(id, ct);
        public Task<bool> UpdateExecutionAsync(ExecutionDocument value, long version, CancellationToken ct) => inner.UpdateExecutionAsync(value, version, ct);
        public Task InsertCallAsync(AgentCallDocument value, CancellationToken ct) => inner.InsertCallAsync(value, ct);
        public async Task<bool> UpdateCallAsync(AgentCallDocument value, long version, CancellationToken ct)
        {
            if (value.ResultadoJson is not null && failure == "before-result") throw new IOException("Synthetic pre-write failure");
            var saved = await inner.UpdateCallAsync(value, version, ct);
            if (value.ResultadoJson is not null && failure == "after-result") throw new IOException("Synthetic acknowledgment loss");
            return saved;
        }
        public Task<IReadOnlyList<AgentCallDocument>> GetCallsAsync(string id, CancellationToken ct) => inner.GetCallsAsync(id, ct);
        public Task<IReadOnlyList<ExecutionDocument>> GetOpenExecutionsAsync(CancellationToken ct) => inner.GetOpenExecutionsAsync(ct);
        public Task SetCallExpirationAsync(string id, DateTime expires, CancellationToken ct) => inner.SetCallExpirationAsync(id, expires, ct);
        public Task PruneExpiredAsync(DateTime now, CancellationToken ct) => inner.PruneExpiredAsync(now, ct);
    }
}
