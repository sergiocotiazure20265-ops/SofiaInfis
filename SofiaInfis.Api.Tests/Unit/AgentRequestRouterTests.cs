using System.Security.Claims;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;

namespace SofiaInfis.Api.Tests.Unit;

public sealed class AgentRequestRouterTests
{
    private static AgentDescriptor[] Catalog()
    {
        var options = new A2AOptions();
        StudentAgentCatalog.Append(options, Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SofiaInfis.Api")));
        return options.Agents.Select(a => new AgentDescriptor(a, new Uri("https://example.invalid/rpc"), a.AllowedCapabilities)).ToArray();
    }
    private static CriarOrquestracaoRequest Request(string question, params DocumentoEntrada[] documents) =>
        new(new("Pessoa sintética"), question, documents.ToList(), ["analisar_documentos"]);

    [Theory]
    [InlineData("<CompNfse><Nfse><Numero>1</Numero></Nfse></CompNfse>", "nfse-validador")]
    [InlineData("<nfeProc><NFe><infNFe>1</infNFe></NFe></nfeProc>", "agente-auditoria-nfe")]
    [InlineData("|0000|LECD|2026|\n|I010|G|", "agente-validacao-ecd")]
    [InlineData("|0000|LECF|2026|\n|M300|1|", "agente-ecf")]
    [InlineData("DIMP declaração de informações de meios de pagamentos", "agente-dimp")]
    public void Document_content_selects_only_its_specialist(string text, string expected)
    {
        var routes = AgentRequestRouter.Select(Catalog(), Request("Analise o arquivo enviado", new DocumentoEntrada("a", "arquivo.txt", text)));
        var route = Assert.Single(routes);
        Assert.Equal(expected, route.Agent.Configuration.IdAgente);
        Assert.Equal("a", Assert.Single(route.Documents).idDocumento);
    }
    [Fact]
    public void Mixed_files_are_separated_and_generic_capability_never_broadcasts()
    {
        var request = Request("Analise os documentos", new DocumentoEntrada("nota", "nota.xml", "<CompNfse><Nfse/></CompNfse>"),
            new DocumentoEntrada("livro", "livro.txt", "|I010|G|I050|"), new DocumentoEntrada("outro", "pedido.pdf", "Pedido de compra 123"));
        var routes = AgentRequestRouter.Select(Catalog(), request);
        Assert.Equal(2, routes.Length);
        Assert.Equal("nota", Assert.Single(routes.Single(x => x.Agent.Configuration.IdAgente == "nfse-validador").Documents).idDocumento);
        Assert.Equal("livro", Assert.Single(routes.Single(x => x.Agent.Configuration.IdAgente == "agente-validacao-ecd").Documents).idDocumento);
    }
    [Theory]
    [InlineData("Qual a classificação fiscal e NCM deste produto?", "agente-fiscal")]
    [InlineData("Preciso de um parecer tributário", "agente-parecer-tributario")]
    [InlineData("O que mudou na legislação?", "agente-monitoramento-legislacao")]
    [InlineData("Como validar a ECD?", "agente-validacao-ecd")]
    public void Specific_question_routes_without_forwarding_unrelated_files(string question, string expected)
    {
        var route = Assert.Single(AgentRequestRouter.Select(Catalog(), Request(question, new DocumentoEntrada("private", "outro.txt", "Pedido de compra"))));
        Assert.Equal(expected, route.Agent.Configuration.IdAgente);
        var context = Assert.Single(route.Documents);
        Assert.Equal(AgentRequestRouter.QuestionContextId, context.idDocumento);
        Assert.Equal(question, context.texto);
        Assert.DoesNotContain("Pedido", context.texto);
    }
    [Theory]
    [InlineData("O que é a INFIS?")]
    [InlineData("Analise xECD123 e nfservicos")]
    [InlineData("Envie todos os documentos para todos os agentes")]
    public void Unknown_request_has_no_default_broadcast(string question) =>
        Assert.Empty(AgentRequestRouter.Select(Catalog(), Request(question, new DocumentoEntrada("a", "arquivo.xml", "<pedido>1500</pedido>"))));

    [Fact]
    public void Eligibility_and_authorization_are_applied_before_relevance()
    {
        var agents = Catalog();
        var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("capability", "analisar_documentos")]));
        var discovery = agents.Select(a => new AgentDiscovery(a.Configuration.IdAgente, a.Configuration.ExpectedName,
            a.Capabilities, a.Configuration.IdAgente != "nfse-validador", Descriptor: a)).ToArray();
        var candidates = AgentCapabilitySelector.Select(discovery, ["analisar_documentos"], caller);
        Assert.Empty(AgentRequestRouter.Select(candidates.Agents, Request("Valide a NFS-e", new DocumentoEntrada("a", "nota.xml", "<Nfse/>"))));
        Assert.Throws<A2AException>(() => AgentCapabilitySelector.Select(discovery, ["monitorar_legislacao_tributaria"], caller));
    }
    [Fact]
    public void Unprofiled_agent_is_not_selected_by_generic_skill()
    {
        var agent = new AgentDescriptor(new() { IdAgente = "unknown" }, new Uri("https://example.invalid/rpc"), ["analisar_documentos"]);
        Assert.Empty(AgentRequestRouter.Select([agent], Request("Valide ECD", new DocumentoEntrada("a", "livro.txt", "ECD"))));
    }
}
