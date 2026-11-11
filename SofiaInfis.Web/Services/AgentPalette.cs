namespace SofiaInfis.Web.Services;

public static class AgentPalette
{
    public static string ClassFor(string id) => "agent-color-" + (id switch
    {
        "nfse-validador" => 0, "agente-validacao-ecd" => 1, "agente-ecf" => 2,
        "agente-fiscal" => 3, "agente-auditoria-nfe" => 4, "agente-dimp" => 5,
        "agente-monitoramento-legislacao" => 6, "agente-parecer-tributario" => 7,
        _ => StableColor(id)
    });
    private static int StableColor(string id)
    {
        var value = 0;
        foreach (var c in id) value = (value * 31 + c) % 8;
        return value;
    }
}
