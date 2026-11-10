using System.Text;
using Microsoft.AspNetCore.Http;
using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Tests.Regression;

public sealed class XmlDocumentTests
{
    private static FormFile File(string text) => new(new MemoryStream(Encoding.UTF8.GetBytes(text)), 0, Encoding.UTF8.GetByteCount(text), "arquivos", "nota.XML");

    [Fact]
    public async Task Fiscal_xml_preserves_namespaces_attributes_and_values()
    {
        var text = await new ExtracaoDocumentoService().ExtrairTextoAsync(File("<NFe xmlns='urn:nfe'><infNFe Id='NFe123'><vNF>123.45</vNF><xProd>Serviço &amp; análise</xProd></infNFe></NFe>"));
        Assert.Contains("vNF", text); Assert.Contains("123.45", text);
        Assert.Contains("NFe123", text); Assert.Contains("urn:nfe", text);
        Assert.Contains("Serviço &amp; análise", text);
    }

    [Theory]
    [InlineData("<NFe><erro></NFe>")]
    [InlineData("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///C:/Windows/win.ini'>]><x>&e;</x>")]
    [InlineData("<!DOCTYPE x SYSTEM 'https://example.invalid/dtd'><x />")]
    public async Task Malformed_xml_and_external_entities_are_rejected(string xml)
        => await Assert.ThrowsAsync<InvalidDataException>(() => new ExtracaoDocumentoService().ExtrairTextoAsync(File(xml)));
}
