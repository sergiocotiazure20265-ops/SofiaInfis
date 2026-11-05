using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Controllers
{
    [Route("api/v1/conhecimentos")]
    [ApiController]
    public class ConhecimentosController (
        ConhecimentoService _service, ILogger<ConhecimentosController> _logger ) : ControllerBase
    {
        /// <summary>
        /// Realiza a ingestão de conhecimentos na base RAG da SOFIA.
        /// </summary>
        [HttpPost("ingest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Ingest(
            [FromBody] IngestConhecimentoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var resultado = await _service.IngestAsync(
                    request,
                    cancellationToken);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Dados inválidos na ingestão de conhecimento.");

                return BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao realizar ingestão de conhecimento.");

                return StatusCode(500, new
                {
                    mensagem = "Erro interno ao registrar conhecimento."
                });
            }
        }

        /// <summary>
        /// Busca conhecimentos por similaridade semântica.
        /// </summary>
        [HttpPost("buscar")]
        [ProducesResponseType(typeof(IEnumerable<ConhecimentoResultado>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Buscar(
            [FromBody] BuscarConhecimentoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var resultados = await _service.BuscarAsync(
                    request,
                    cancellationToken);

                return Ok(resultados);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Dados inválidos na busca de conhecimentos.");

                return BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao buscar conhecimentos.");

                return StatusCode(500, new
                {
                    mensagem = "Erro interno ao consultar conhecimentos."
                });
            }
        }
    }
}
