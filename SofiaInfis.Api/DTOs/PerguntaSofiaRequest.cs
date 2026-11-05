using System.ComponentModel.DataAnnotations;

namespace SofiaInfis.Api.DTOs
{
    public record PerguntaSofiaRequest(
        [Required(ErrorMessage = "A pergunta é obrigatória.")]
        [StringLength(
            2000,
            MinimumLength = 5,
            ErrorMessage = "A pergunta deve ter entre 5 e 2000 caracteres.")]
        string pergunta,

        List<IFormFile>? arquivos = null

        );
}
