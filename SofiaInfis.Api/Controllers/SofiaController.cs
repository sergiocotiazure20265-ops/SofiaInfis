using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Controllers
{
    [Route("api/v1/sofia")]
    [ApiController]
    public class SofiaController(
            SofiaService _sofiaService,
            ILogger<SofiaController> _logger
        ) : ControllerBase
    {
        /// <summary>
        /// Envia uma pergunta e documentos para análise
        /// pela SOFIA utilizando RAG e OpenAI.
        /// </summary>
        [HttpPost("perguntar")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
        [ProducesResponseType(typeof(RespostaSofia), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Perguntar([FromForm] PerguntaSofiaRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.pergunta))
                {
                    return BadRequest(new
                    {
                        mensagem = "A pergunta é obrigatória."
                    });
                }

                var resposta =
                    await _sofiaService.PerguntarAsync(
                        request,
                        cancellationToken);

                return Ok(resposta);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Dados inválidos na requisição da SOFIA.");

                return BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Documento inválido recebido pela SOFIA.");

                return BadRequest(new
                {
                    mensagem = "Arquivo inválido ou não suportado."
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento da requisição pelo cliente
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro no processamento da SOFIA.");

                return StatusCode(500, new
                {
                    mensagem =
                        "Não foi possível processar a solicitação."
                });
            }
        }
    }
}
