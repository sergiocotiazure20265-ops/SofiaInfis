using System.Text;
using Microsoft.AspNetCore.Http;
using SofiaInfis.Api.Services;
namespace SofiaInfis.Api.Tests.Regression;

public sealed class LegacyDocumentExtractionTests
{
    [Fact]
    public async Task Real_Kestrel_preserves_twenty_MiB_request_limit()
    {
        await using var factory = new Fixtures.SofiaApiFactory();
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent { { new StringContent("Pergunta sintética"), "pergunta" } };
        var bytes = Enumerable.Repeat((byte)'a', 5 * 1024 * 1024).ToArray();
        for (var i = 0; i < 4; i++) form.Add(new ByteArrayContent(bytes), "arquivos", $"a{i}.txt");
        // Four legal files plus multipart headers exceed the endpoint's total limit.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sofia/perguntar") { Content = form };
        request.Headers.ExpectContinue = true;
        using var response = await client.SendAsync(request);
        // MVC preserves the existing 400 ProblemDetails for a form-body read failure.
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Request body too large", await response.Content.ReadAsStringAsync());
    }
    private static FormFile File(byte[] bytes, string name) => new(new MemoryStream(bytes), 0, bytes.Length, "arquivos", name);
    [Fact]
    public async Task Txt_limits_and_truncation_are_preserved()
    {
        var service = new ExtracaoDocumentoService();
        Assert.Equal("Olá sintético", await service.ExtrairTextoAsync(File(Encoding.UTF8.GetBytes("Olá sintético"), "a.txt")));
        Assert.Equal(new string('a', 20000) + "\n[DOCUMENTO TRUNCADO]", await service.ExtrairTextoAsync(File(Encoding.UTF8.GetBytes(new string('a', 20001)), "a.txt")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExtrairTextoAsync(File([], "a.txt")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExtrairTextoAsync(File([65], "a.exe")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExtrairTextoAsync(File(new byte[5 * 1024 * 1024 + 1], "a.txt")));
    }
    [Fact]
    public async Task Pdf_is_extracted_and_six_files_are_rejected_before_Rag()
    {
        var builder = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText("Documento sintetico PDF", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        Assert.Contains("Documento sintetico PDF", await new ExtracaoDocumentoService().ExtrairTextoAsync(File(builder.Build(), "a.pdf")));
        await using var factory = new Fixtures.SofiaApiFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent { { new StringContent("Pergunta sintética"), "pergunta" } };
        for (var i = 0; i < 6; i++) form.Add(new ByteArrayContent([65]), "arquivos", $"a{i}.txt");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/sofia/perguntar", form)).StatusCode);
    }
}
