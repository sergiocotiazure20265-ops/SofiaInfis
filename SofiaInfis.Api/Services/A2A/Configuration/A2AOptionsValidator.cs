namespace SofiaInfis.Api.Services.A2A.Configuration;

public static class A2AOptionsValidator
{
    public static bool IsValid(A2AOptions options)
    {
        if (!options.Enabled) return true;
        if (new[] { options.DiscoveryTimeoutSeconds, options.CallTimeoutSeconds, options.ExecutionTimeoutSeconds,
            options.FinalizationTimeoutSeconds, options.PollingMilliseconds, options.MaxEnvelopeBytes,
            options.MaxActiveExecutions, options.MaxPendingExecutions, options.MaxConcurrentCalls, options.RetentionDays }.Any(x => x <= 0)) return false;
        if (options.Agents.Select(x => x.IdAgente).Distinct(StringComparer.Ordinal).Count() != options.Agents.Length) return false;
        return options.InboundClients.All(x => !string.IsNullOrWhiteSpace(x.ClientId) && x.TokenHash.Length == 64 && x.TokenHash.All(Uri.IsHexDigit))
            && options.InboundClients.Select(x => x.TokenHash.ToUpperInvariant()).Distinct().Count() == options.InboundClients.Length
            && options.Agents.All(x => !string.IsNullOrWhiteSpace(x.IdAgente) && x.IdAgente.Length <= 200 &&
                !string.IsNullOrWhiteSpace(x.ExpectedName) && x.ExpectedName.Length <= 200 &&
                Uri.TryCreate(x.CardUri, UriKind.Absolute, out _) && x.AllowedDestinations.Length > 0 &&
                x.AllowedAddresses.Length > 0);
    }
}
