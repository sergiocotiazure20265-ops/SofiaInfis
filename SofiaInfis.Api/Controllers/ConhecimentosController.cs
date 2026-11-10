using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SofiaInfis.Api.DTOs;
using SofiaInfis.Api.Services;

namespace SofiaInfis.Api.Controllers
{
    [Route("api/v1/conhecimentos")]
    [ApiController]
    public class ConhecimentosController (
        ConhecimentoService _service, ILogger<ConhecimentosController> _logger, ExtracaoDocumentoService extraction ) : ControllerBase
    {
        /// <summary>Ingere um PDF textual, TXT ou XML na base de conhecimentos.</summary>
        [HttpPost("ingest/arquivo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> IngestArquivo([FromForm] IngestArquivoRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var text = await extraction.ExtrairTextoAsync(request.arquivo, cancellationToken);
                if (text.EndsWith("\n[DOCUMENTO TRUNCADO]", StringComparison.Ordinal))
                    return BadRequest(new { mensagem = "O documento excede 20.000 caracteres. Divida-o para ingerir o conteúdo completo." });
                if (text.Length < 20)
                    return BadRequest(new { mensagem = "O documento precisa conter ao menos 20 caracteres de texto." });
                return Ok(await _service.IngestAsync(new(request.titulo, text, request.fonte), cancellationToken));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
            { return BadRequest(new { mensagem = "Arquivo inválido. Use PDF textual, TXT ou XML válido de até 5 MB e 20.000 caracteres." }); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na ingestão de arquivo.");
                return StatusCode(500, new { mensagem = "Não foi possível registrar o documento." });
            }
        }

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
