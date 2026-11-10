using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Services;
using SofiaInfis.Api.Tests.Fixtures;
using SofiaInfis.Web.Services;

namespace SofiaInfis.Api.Tests.Web;

public sealed class SofiaRagIntegrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("referencia.txt", "A referência fiscal sintética deve ser consultada antes da conclusão.")]
    [InlineData("referencia.xml", "<referencia><assunto>Revisão fiscal sintética</assunto><valor>150</valor></referencia>")]
    public async Task Upload_search_and_chat_work_without_agents(string name, string content)
    {
        await using var api = new SofiaApiFactory(new() { ["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString });
        using var transport = api.CreateClient();
        var gateway = new LiveSofiaChat(transport, Options.Create(new SofiaOptions { ApiBaseUrl = "http://localhost/" }), new(new ExtracaoDocumentoService()));
        Assert.Empty(await gateway.GetAgentsAsync(default));
        var file = new ChatFile(name, Encoding.UTF8.GetBytes(content));
        var receipt = await gateway.IngestKnowledgeAsync("Referência de teste", "Teste sintético", "", file, default);
        Assert.Equal(1, receipt.totalChunks);
        var results = await transport.PostAsJsonAsync("/api/v1/conhecimentos/buscar", new BuscarConhecimentoRequest("Revisão fiscal", 3));
        results.EnsureSuccessStatusCode();
        var matches = await results.Content.ReadFromJsonAsync<ConhecimentoResultado[]>();
        Assert.Contains(matches!, m => m.texto.Contains(name.EndsWith("xml") ? "150" : "conclusão"));
        var run = new ChatRun("Analise a referência fiscal", [name]);
        await gateway.SubmitAsync(new("Pessoa", run.Question, [file]), run, () => Task.CompletedTask, default);
        Assert.Equal("concluida", run.State); Assert.NotNull(run.Answer); Assert.Empty(run.Agents);
        Assert.NotNull(run.ExecutionId);
    }

    [Theory]
    [InlineData("bad.xml", "<broken>")]
    [InlineData("large.txt", null)]
    public async Task Invalid_or_truncated_documents_are_not_persisted(string name, string? content)
    {
        await using var api = new SofiaApiFactory(new() { ["ConnectionStrings:PostgreSQL"] = postgres.ConnectionString });
        using var client = api.CreateClient();
        using var form = new MultipartFormDataContent
        {
            {new StringContent("Documento inválido"), "titulo"}, {new StringContent("Teste"), "fonte"},
            {new ByteArrayContent(Encoding.UTF8.GetBytes(content ?? new string('a', 20001))), "arquivo", name}
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/conhecimentos/ingest/arquivo", form)).StatusCode);
    }
}
