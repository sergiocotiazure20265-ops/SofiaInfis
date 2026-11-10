using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Tests.Fixtures;
namespace SofiaInfis.Api.Tests.Integration;

public sealed class PostmanSimulationTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Actual_sofia_client_interoperates_with_local_Postman_simulation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        using (var port = new TcpListener(IPAddress.Loopback, 5190)) { port.Start(); port.Stop(); }
        var start = new ProcessStartInfo("cmd.exe") { WorkingDirectory = root, CreateNoWindow = true,
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("/d"); start.ArgumentList.Add("/c");
        start.ArgumentList.Add("postman simulation run postman/simulations/sofia-a2a/sofia-a2a/sofia-a2a.sim.yaml --port 5190 --no-history --output ndjson");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        var credentialRef = "SOFIA_POSTMAN_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        Environment.SetEnvironmentVariable(credentialRef, "postman-local-test-only");
        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            var ready = false;
            for (var i = 0; i < 100 && !process.HasExited; i++)
            {
                try { ready = (await probe.GetAsync("http://127.0.0.1:5190/fiscal/.well-known/agent-card.json")).IsSuccessStatusCode; }
                catch (HttpRequestException) { } catch (TaskCanceledException) { }
                if (ready) break;
                await Task.Delay(100);
            }
            Assert.True(ready, "Local Postman simulation did not start; verify CLI installation and port 5190.");
            using var config = new A2ATestConfiguration(mongo);
            config.Settings["A2A:Agents:0:IdAgente"] = "postman-fiscal";
            config.Settings["A2A:Agents:0:ExpectedName"] = "postman-fiscal";
            config.Settings["A2A:Agents:0:CardUri"] = "http://127.0.0.1:5190/fiscal/.well-known/agent-card.json";
            config.Settings["A2A:Agents:0:AllowedDestinations:0"] = config.Settings["A2A:Agents:0:CardUri"];
            config.Settings["A2A:Agents:0:AllowedDestinations:1"] = "http://127.0.0.1:5190/fiscal/rpc";
            config.Settings["A2A:Agents:0:AllowedAddresses:0"] = "127.0.0.1";
            config.Settings["A2A:Agents:0:AllowedCapabilities:0"] = "analisar_documentos";
            config.Settings["A2A:Agents:0:CredentialRef"] = credentialRef;
            await using var factory = new SofiaApiFactory(config.Settings);
            using var client = config.Client(factory);
            var id = await PartialOrchestrationTests.Start(client);
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
            var execution = await PartialOrchestrationTests.Wait(repository, id);
            var calls = await repository.GetCallsAsync(id, default);
            Assert.True(execution.Estado == "concluida", "State: " + execution.Estado + "; failure: " + execution.FalhaRegistro + "; calls: " + string.Join(",", calls.Select(x => x.FalhaTecnica?.codigo)));
            Assert.Contains(id, Assert.Single(await repository.GetCallsAsync(id, default)).ResultadoJson!);
        }
        finally
        {
            Environment.SetEnvironmentVariable(credentialRef, null);
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(output, error);
        }
    }
}
