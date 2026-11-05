using System.ComponentModel.DataAnnotations;

namespace SofiaInfis.Api.DTOs
{
    public record IngestConhecimentoRequest(
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(
            200,
            MinimumLength = 5,
            ErrorMessage = "O título deve conter entre 5 e 200 caracteres.")]
        string titulo,

        [Required(ErrorMessage = "O texto é obrigatório.")]
        [StringLength(
            100000,
            MinimumLength = 20,
            ErrorMessage = "O texto deve conter entre 20 e 100.000 caracteres.")]
        string texto,

        [Required(ErrorMessage = "A fonte é obrigatória.")]
        [StringLength(
            500,
            MinimumLength = 3,
            ErrorMessage = "A fonte deve conter entre 3 e 500 caracteres.")]
        string fonte
    );
}