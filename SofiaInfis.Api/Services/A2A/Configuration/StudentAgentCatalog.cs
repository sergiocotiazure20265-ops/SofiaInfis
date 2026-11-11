namespace SofiaInfis.Api.Services.A2A.Configuration;

/// <summary>Append reviewed defaults; explicit A2A:Agents entries always take precedence.</summary>
public static class StudentAgentCatalog
{
    public static void Append(A2AOptions options, string contentRoot)
    {
        var root = File.Exists(Path.Combine(contentRoot, "a2a-student-agents.json"))
            ? contentRoot : Path.GetDirectoryName(typeof(StudentAgentCatalog).Assembly.Location)!;
        var catalog = new ConfigurationBuilder().SetBasePath(root)
            .AddJsonFile("a2a-student-agents.json", optional: false).Build();
        using var lifetime = (IDisposable)catalog;
        var students = catalog.GetSection("A2A:Agents").Get<AgentOptions[]>() ?? [];
        foreach (var configured in options.Agents)
        {
            var profile = students.FirstOrDefault(x => x.IdAgente == configured.IdAgente);
            if (profile is null) continue;
            configured.Developer ??= profile.Developer;
            if (configured.RoutingTerms.Length == 0) configured.RoutingTerms = profile.RoutingTerms;
            if (configured.DocumentTerms.Length == 0) configured.DocumentTerms = profile.DocumentTerms;
        }
        var existing = options.Agents.Select(x => x.IdAgente).ToHashSet(StringComparer.Ordinal);
        options.Agents = [.. options.Agents, .. students.Where(x => existing.Add(x.IdAgente))];
    }
}
