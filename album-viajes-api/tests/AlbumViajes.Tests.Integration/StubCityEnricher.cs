using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Integration;

/// <summary>
/// Sustituye a Wikipedia y Wikidata en los tests: la suite no puede depender de
/// una red ni del contenido real de esas fuentes.
/// </summary>
public sealed class StubCityEnricher : ICityEnricher
{
    public Result<CityFacts> NextResult { get; set; } = Default;

    private static Result<CityFacts> Default => new CityFacts(
        2_427_129,
        "Wikidata (Q48278), 2020",
        "Capital del departamento de Antioquia.",
        "Q48278",
        "https://es.wikipedia.org/wiki/Medellin");

    public void Reset() => NextResult = Default;

    public void FailWith(Error error) => NextResult = Result<CityFacts>.Failure(error);

    public Task<Result<CityFacts>> FetchAsync(
        string cityName,
        CountryCode countryCode,
        Coordinates coordinates,
        CancellationToken cancellationToken) => Task.FromResult(NextResult);
}
