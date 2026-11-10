using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Transport;
using SofiaInfis.Api.Tests.Fixtures;
using SofiaInfis.Web.Services;
using Xunit.Abstractions;

namespace SofiaInfis.Api.Tests.Integration;

/// <summary>Explicit opt-in: external calls contain only the synthetic fixture below.</summary>
public sealed class StudentAgentsLiveFactAttribute : FactAttribute
{
    public StudentAgentsLiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SOFIA_TEST_LIVE_STUDENTS") != "1")
            Skip = "External student endpoints: opt in with SOFIA_TEST_LIVE_STUDENTS=1 and SOFIA_A2A_TOKEN.";
    }
}

public sealed class StudentAgentsLiveTests(MongoFixture mongo, ITestOutputHelper output) : IClassFixture<MongoFixture>
{
    [StudentAgentsLiveFact]
    public async Task Synthetic_document_reaches_published_students_through_frontend_gateway_and_orchestrator()
    {
        Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SOFIA_A2A_TOKEN")));
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var settings = new Dictionary<string, string?>
        {
            ["A2A:Enabled"] = "true", ["A2A:IncludeStudentAgents"] = "true",
            ["A2A:DiscoveryTimeoutSeconds"] = "15",
            ["A2A:MongoConnectionString"] = mongo.ConnectionString, ["A2A:MongoDatabase"] = mongo.DatabaseName,
            ["A2A:InboundClients:0:ClientId"] = "student-live-probe",
            ["A2A:InboundClients:0:TokenHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            ["A2A:InboundClients:0:Capabilities:0"] = "analisar_documentos"
        };
        settings["A2A:InboundClients:0:Capabilities:1"] = "monitorar_legislacao_tributaria";
        var permissions = new[] { "a2a:agents:read", "a2a:execute", "a2a:read", "a2a:cancel" };
        for (var i = 0; i < permissions.Length; i++) settings[$"A2A:InboundClients:0:Permissions:{i}"] = permissions[i];
        await using var api = new SofiaApiFactory(settings);
        var authentication = new List<object>();
        var configuration = api.Services.GetRequiredService<A2AOptions>();
        var clients = api.Services.GetRequiredService<A2AHttpClientFactory>();
        foreach (var agent in configuration.Agents)
        {
            using var remote = clients.Create(agent);
            remote.Timeout = TimeSpan.FromSeconds(20);
            foreach (var auth in new[] { "none", "invalid", "valid" })
            {
                using var cardRequest = new HttpRequestMessage(HttpMethod.Get, agent.CardUri);
                if (auth != "none") cardRequest.Headers.Authorization = new("Bearer", auth == "valid" ? Environment.GetEnvironmentVariable("SOFIA_A2A_TOKEN") : "synthetic-invalid-token");
                using var cardResponse = await remote.SendAsync(cardRequest);
                using var rpcRequest = new HttpRequestMessage(HttpMethod.Post, agent.AllowedDestinations[1])
                {
                    Content = new StringContent(A2A03JsonCodec.TaskRequest("tasks/get", Guid.NewGuid().ToString(), "synthetic-nonexistent-task"), Encoding.UTF8, "application/json")
                };
                rpcRequest.Headers.Authorization = cardRequest.Headers.Authorization;
                using var rpcResponse = await remote.SendAsync(rpcRequest);
                authentication.Add(new { agent.IdAgente, auth, cardStatus = (int)cardResponse.StatusCode, rpcStatus = (int)rpcResponse.StatusCode });
                if (auth == "valid")
                {
                    Assert.Equal(HttpStatusCode.OK, cardResponse.StatusCode);
                    if (agent.IdAgente is "nfse-validador" or "agente-validacao-ecd")
                        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
                }
                else
                {
                    Assert.Contains(cardResponse.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
                    // New deployments may expose a card before their RPC is implemented.
                }
            }
        }
        output.WriteLine(JsonSerializer.Serialize(authentication));
        using var transport = api.CreateClient();
        using var ready = api.CreateClient();
        ready.DefaultRequestHeaders.Authorization = new("Bearer", token);
        for (var i = 0; i < 100; i++)
        {
            if ((await ready.GetAsync("/api/v1/agentes")).StatusCode != HttpStatusCode.ServiceUnavailable) break;
            await Task.Delay(100);
        }
        using var unauthorized = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await unauthorized.GetAsync("/api/v1/agentes")).StatusCode);
        var gateway = new LiveSofiaChat(transport, Options.Create(new SofiaOptions
        {
            UseSpecialistAgents = true, ApiBaseUrl = "http://localhost/", ServiceToken = token
        }), new(new SofiaInfis.Api.Services.ExtracaoDocumentoService()));
        var catalog = await gateway.GetAgentsAsync(default);
        output.WriteLine(JsonSerializer.Serialize(catalog));
        var input = new ChatInput("Teste sintético SOFIA", "Valide a estrutura deste documento sintético de treinamento.",
            [new("ecd-sintetico.txt", Encoding.UTF8.GetBytes("|0000|LECD|01012025|31122025|EMPRESA FICTICIA TESTE|00000000000000|SP|000000000|3550308||0|0|0|0|0|\n|I050|01012025|01|A|1|1||Ativo|\n|I150|01012025|31122025|\n|I155|1||100,00|D|0,00|0,00|100,00|D|\n|9999|5|")),
             new("nfse-sintetica.xml", Encoding.UTF8.GetBytes("<CompNfse><Nfse versao=\"2.04\"><InfNfse Id=\"sintetico-001\"><Numero>1</Numero><DataEmissao>2026-11-10T00:00:00</DataEmissao><PrestadorServico><RazaoSocial>EMPRESA FICTICIA TESTE</RazaoSocial></PrestadorServico><Servico><Valores><ValorServicos>100.00</ValorServicos></Valores><Discriminacao>DOCUMENTO SINTETICO SEM VALIDADE FISCAL</Discriminacao></Servico></InfNfse></Nfse></CompNfse>"))]);
        var run = new ChatRun(input.Question, input.Files.Select(x => x.Name).ToArray());
        await gateway.SubmitAsync(input, run, () => Task.CompletedTask, default);
        // Sanitized evidence: IDs/states only; never persist documents, names, analyses or tokens.
        var report = new { timestamp = DateTimeOffset.UtcNow, authentication, catalog, run.ExecutionId, run.State,
            agents = run.Agents.Select(a => new { a.Id, a.State, a.Error, hasResult = a.Result != null,
                idExecucao = a.Result?.idExecucao, idSolicitacao = a.Result?.idSolicitacao,
                status = a.Result?.status, findings = a.Result?.achados.Count }) };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        output.WriteLine(json);
        if (Environment.GetEnvironmentVariable("SOFIA_A2A_REPORT") is { Length: > 0 } path)
            await File.WriteAllTextAsync(path, json);
        Assert.Equal(configuration.Agents.Length, catalog.Count);
        var originalAgents = run.Agents.Where(a => a.Id is "nfse-validador" or "agente-validacao-ecd").ToArray();
        Assert.Equal(2, originalAgents.Length);
        Assert.All(originalAgents, agent =>
        {
            Assert.NotNull(agent.Result);
            Assert.Equal(run.ExecutionId, agent.Result.idExecucao.ToString());
            Assert.NotEqual(Guid.Empty, agent.Result.idSolicitacao);
            Assert.NotEmpty(agent.Result.analise);
        });
        var successful = run.Agents.Where(a => a.Result is not null).ToArray();
        Assert.Equal(successful.Length, successful.Select(a => a.Result!.idSolicitacao).Distinct().Count());
    }
}
