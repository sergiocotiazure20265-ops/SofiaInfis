using SofiaInfis.Api.DTOs.A2A;

namespace SofiaInfis.Web.Services;

// Presentation directory only. Listing an agent never authorizes an A2A call.
public static class SidebarAgentDirectory
{
    public const string PendingConnection = "Conexão pendente";

    private static readonly AgenteDisponivel[] Students =
    [
        Pending("nfse-validador", "Agente Validador NFS-e", "Marcos Vinícius Ricardo Pinto"),
        Pending("agente-validacao-ecd", "Agente Validação ECD", "Caroline Bartholo de Biasi"),
        Pending("agente-parecer-tributario", "Agente Parecer Tributário", "Roberto Rabelo Gonzaga Junior"),
        Pending("agente-ecf", "Agente Validação ECF", "Kaio Sodré Macedo"),
        Pending("agente-fiscal", "Agente Classificação Fiscal de Produtos", "Marcus Paulo Simões da Silva Neves de Oliveira"),
        Pending("agente-auditoria-nfe", "Agente Validação NF-e", "Andressa Cristina Cândido Firmino da Silva"),
        Pending("agente-dimp", "Agente DIMP", "Matheus Machado Pinheiro do Valle"),
        Pending("agente-monitoramento-legislacao", "Agente Monitoramento Legislação", "Lettícia Luize Ferreira Ribeiro Santos"),
        Pending("agente-sped-fiscal", "Agente SPED Fiscal", "Gabriel de Freitas Maurity"),
        Pending("agente-sped-contribuicoes", "Agente SPED Contribuições", "Vitor Guimaraes Vianna")
    ];

    private static AgenteDisponivel Pending(string id, string name, string developer) =>
        new(id, name, [], false, PendingConnection, developer);

    public static IReadOnlyList<AgenteDisponivel> Merge(IReadOnlyList<AgenteDisponivel> connected)
    {
        var result = connected.Select(agent => agent with
        {
            desenvolvedor = Students.FirstOrDefault(student => student.idAgente == agent.idAgente)?.desenvolvedor
                ?? agent.desenvolvedor
        }).ToList();
        var ids = connected.Select(agent => agent.idAgente).ToHashSet(StringComparer.Ordinal);
        result.AddRange(Students.Where(student => !ids.Contains(student.idAgente)));
        return result;
    }
}
