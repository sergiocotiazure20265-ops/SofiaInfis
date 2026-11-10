using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class A2ASecurityBoundaryTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Identity_permission_and_ownership_are_independent_of_user_name()
    {
        await using var agent = await A2AAgentServer.StartAsync();
        using var configuration = new A2ATestConfiguration(mongo, agent);
        var otherToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        configuration.Settings["A2A:InboundClients:1:ClientId"] = "other-client";
        configuration.Settings["A2A:InboundClients:1:TokenHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otherToken)));
        configuration.Settings["A2A:InboundClients:1:Permissions:0"] = "a2a:read";
        configuration.Settings["A2A:InboundClients:1:Permissions:1"] = "a2a:cancel";
        var deniedToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        configuration.Settings["A2A:InboundClients:2:ClientId"] = "no-permissions";
        configuration.Settings["A2A:InboundClients:2:TokenHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deniedToken)));
        await using var factory = new SofiaApiFactory(configuration.Settings);
        using var owner = configuration.Client(factory);
        var id = await PartialOrchestrationTests.Start(owner);
        using var other = factory.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/v1/orquestracoes/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync("/api/v1/orquestracoes/" + id + "/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/orquestracoes/{id}/chamadas/{Guid.NewGuid()}/resultado")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request)).StatusCode);
        using var anonymous = factory.CreateClient();
        foreach (var path in new[] { "/api/v1/agentes", "/api/v1/orquestracoes/" + id,
            "/api/v1/orquestracoes/" + id + "/chamadas/" + Guid.NewGuid() + "/resultado" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/v1/orquestracoes/" + id + "/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request)).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-synthetic-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/agentes")).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deniedToken);
        foreach (var path in new[] { "/api/v1/agentes", "/api/v1/orquestracoes/" + id, $"/api/v1/orquestracoes/{id}/chamadas/{Guid.NewGuid()}/resultado" })
            Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.PostAsync("/api/v1/orquestracoes/" + id + "/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.PostAsJsonAsync("/api/v1/orquestracoes", SingleAgentAnalysisTests.Request)).StatusCode);
        using var invalid = await owner.PostAsJsonAsync("/api/v1/orquestracoes", new { usuario = new { nome = "Pessoa" } });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("INVALID_ANALYSIS_REQUEST", await invalid.Content.ReadAsStringAsync());
        // Finish the authorized call before measuring whether the forbidden request
        // invokes the agent. Otherwise the background worker can increment this
        // counter during the authorization assertion.
        using var scope = factory.Services.CreateScope();
        await PartialOrchestrationTests.Wait(scope.ServiceProvider.GetRequiredService<IExecutionRepository>(), id);
        var callsBefore = agent.AnalysisCalls;
        using var denied = await owner.PostAsJsonAsync("/api/v1/orquestracoes", new
        { usuario = new { nome = "Pessoa" }, pergunta = "Pergunta sintética", documentos = new[] { new { idDocumento = "a", nome = "a.txt", texto = "texto" } }, capacidades = new[] { "nao_autorizada" } });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(callsBefore, agent.AnalysisCalls);
        var rotated = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        factory.Services.GetRequiredService<A2AOptions>().InboundClients[0].TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rotated)));
        using var renewed = factory.CreateClient();
        renewed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated);
        Assert.Equal(HttpStatusCode.OK, (await renewed.GetAsync("/api/v1/orquestracoes/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/v1/orquestracoes/" + id)).StatusCode);
    }
}
