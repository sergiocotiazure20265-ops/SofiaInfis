using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SofiaInfis.Api.Services;

public sealed class RagHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(configuration.GetConnectionString("PostgreSQL"));
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT embedding FROM conhecimentos LIMIT 0", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch { return HealthCheckResult.Unhealthy("Base de conhecimentos indisponível."); }
    }
}
