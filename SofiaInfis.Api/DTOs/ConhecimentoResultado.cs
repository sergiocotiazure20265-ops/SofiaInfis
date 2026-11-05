namespace SofiaInfis.Api.DTOs
{
    public record ConhecimentoResultado(
            Guid id,
            string titulo,
            string texto,
            string fonte,
            int IndiceChunk,
            double Similaridade
        );
}
