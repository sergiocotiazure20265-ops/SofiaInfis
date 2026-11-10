using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Configuration;
using SofiaInfis.Api.Services.A2A.Domain;
using SofiaInfis.Api.Services.A2A.Persistence;
namespace SofiaInfis.Api.Services.A2A;

public sealed class NoEligibleAgentExecutionRecorder(IExecutionRepository repository, TimeProvider time, A2AOptions options)
{
    public async Task CompleteAsync(ExecutionDocument execution, string[] missing, CancellationToken ct)
    {
        var expected = execution.Version;
        execution.SemCobertura = missing;
        execution.Estado = ExecutionStates.Failed;
        execution.EncerradoEm = time.GetUtcNow().UtcDateTime;
        execution.ExpiresAt = execution.EncerradoEm.Value.AddDays(options.RetentionDays);
        if (!await repository.UpdateExecutionAsync(execution, expected, ct)) throw new A2AException("armazenamento", "EXECUTION_WRITE_CONFLICT", 503);
    }
}
