using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using SofiaInfis.Api.DTOs.A2A;
using SofiaInfis.Api.Services.A2A.Abstractions;
using SofiaInfis.Api.Services.A2A.Security;
namespace SofiaInfis.Api.Controllers;

[ApiController]
[EnableCors("SofiaBrowser")]
[Route("api/v1/agentes")]
[Authorize(Policy = "a2a:agents:read")]
[ServiceFilter(typeof(A2AEnabledResourceFilter))]
[ServiceFilter(typeof(A2AExceptionFilter))]
public sealed class AgentesController(IAgentDiscovery discovery) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var agents = await discovery.DiscoverAsync(User, cancellationToken);
        return Ok(new { items = agents.Select(x => new AgenteDisponivel(x.idAgente, x.nome, x.capacidades, x.elegivel, x.motivo)) });
    }
}
