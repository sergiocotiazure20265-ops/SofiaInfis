using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Security;
using SofiaInfis.Api.Services.A2A.Domain;
using System.Text.Json;
namespace SofiaInfis.Api.Controllers;

[ApiController]
[EnableCors("SofiaBrowser")]
[Route("api/v1/orquestracoes")]
[ServiceFilter(typeof(A2AEnabledResourceFilter))]
[ServiceFilter(typeof(A2AExceptionFilter))]
public sealed class OrquestracoesController(IOrchestrationService service, IExecutionRepository repository,
    ExecutionCursorProtector cursors, TimeProvider time) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "a2a:execute")]
    public async Task<IActionResult> Start([FromBody] CriarOrquestracaoRequest request, CancellationToken cancellationToken)
    {
        var execution = await service.StartAsync(request, User, cancellationToken);
        var location = "/api/v1/orquestracoes/" + execution.Id;
        return Accepted(location, new OrquestracaoAceita(execution.Id, "em_andamento", execution.CriadoEm, true, location));
    }
    [HttpPost("{idExecucao}/cancelar")]
    [Authorize(Policy = "a2a:cancel")]
    public async Task<IActionResult> Cancel(string idExecucao, CancellationToken cancellationToken)
    {
        var execution = await service.CancelAsync(idExecucao, User, cancellationToken);
        return Ok(new OrquestracaoCancelada(execution.Id, execution.Estado, true));
    }
    [HttpGet("{idExecucao}")]
    [Authorize(Policy = "a2a:read")]
    public async Task<IActionResult> Get(string idExecucao, [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var execution = await service.GetAsync(idExecucao, User, cancellationToken);
        if (pageSize is < 1 or > 100) throw new A2AException("validacao", "INVALID_PAGE_SIZE");
        var offset = cursors.Decode(cursor, A2AAuthorizationPolicies.ClientId(User), execution.Id);
        var calls = await repository.GetCallsAsync(execution.Id, cancellationToken);
        if (offset > calls.Count) throw new A2AException("validacao", "INVALID_CURSOR");
        var page = calls.Skip(offset).Take(pageSize).Select(call =>
        {
            string? status = null;
            if (call.ResultadoJson is not null)
            {
                using var result = JsonDocument.Parse(call.ResultadoJson);
                status = result.RootElement.GetProperty("status").GetString();
            }
            return new ChamadaResumo(call.Id, call.IdAgente, call.Estado, status, call.ResultadoJson is not null,
                call.ResultadoJson is null ? null : "/api/v1/orquestracoes/" + execution.Id + "/chamadas/" + call.Id + "/resultado",
                call.FalhaTecnica, call.DuracaoMs, call.Consumo, call.MotivoSelecao, call.DocumentosEnviados);
        }).ToArray();
        var next = offset + page.Length < calls.Count ? cursors.Encode(execution.OwnerClientId, execution.Id, offset + page.Length) : null;
        return Ok(new ExecucaoConsulta(execution.Id, execution.Estado, execution.CriadoEm, execution.EncerradoEm,
            execution.ExpiresAt, execution.CapacidadesSolicitadas, execution.SemCobertura,
            new(page, next), true, execution.FalhaRegistro));
    }
    [HttpGet("{idExecucao}/chamadas/{idSolicitacao}/resultado")]
    [Authorize(Policy = "a2a:read")]
    public async Task<IActionResult> Result(string idExecucao, string idSolicitacao, CancellationToken cancellationToken)
    {
        var execution = await service.GetAsync(idExecucao, User, cancellationToken);
        var calls = await repository.GetCallsAsync(execution.Id, cancellationToken);
        var call = calls.SingleOrDefault(x => x.Id == idSolicitacao && x.ExpiresAt > time.GetUtcNow().UtcDateTime)
            ?? throw new A2AException("validacao", "RESULT_NOT_FOUND", 404);
        if (call.ResultadoJson is null) throw new A2AException("validacao", "RESULT_NOT_AVAILABLE", 409);
        return Content(call.ResultadoJson, "application/json; charset=utf-8");
    }
}
