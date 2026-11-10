namespace SofiaInfis.Api.Services.A2A.Persistence;
public record ConsumoInformado(long? tokensEntrada = null, long? tokensSaida = null, long? tokensTotal = null,
    decimal? custoValor = null, string? custoMoeda = null, string? origem = null);
