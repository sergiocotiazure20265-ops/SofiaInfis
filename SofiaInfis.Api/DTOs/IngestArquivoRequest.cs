using System.ComponentModel.DataAnnotations;

namespace SofiaInfis.Api.DTOs;

public sealed class IngestArquivoRequest
{
    [Required, StringLength(200, MinimumLength = 5)]
    public string titulo { get; set; } = "";
    [Required, StringLength(500, MinimumLength = 3)]
    public string fonte { get; set; } = "";
    [Required]
    public IFormFile arquivo { get; set; } = null!;
}
