using System.ComponentModel.DataAnnotations;

namespace SofiaInfis.Api.DTOs
{
    public record BuscarConhecimentoRequest(
        [Required(ErrorMessage = "A pergunta é obrigatória.")]
        [StringLength(
            2000,
            MinimumLength = 5,
            ErrorMessage = "A pergunta deve conter entre 5 e 2.000 caracteres.")]
        string Pergunta,

        [Range(
            1,
            20,
            ErrorMessage = "A quantidade de resultados deve estar entre 1 e 20.")]
        int TopK = 3
    );
}