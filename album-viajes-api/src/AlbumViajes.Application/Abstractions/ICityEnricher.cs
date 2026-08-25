using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Datos que las fuentes externas saben dar de una ciudad. La comida tipica no
/// esta aqui a proposito: ni Wikidata ni Wikipedia la publican como dato.
/// </summary>
public sealed record CityFacts(
    long? Population,
    string? PopulationSource,
    string? ShortDescription,
    string? WikidataId,
    string? WikipediaUrl);

/// <summary>
/// Puerto hacia las fuentes externas de datos de ciudades. La capa de
/// aplicacion no sabe si detras hay HTTP, un cache o un archivo.
/// </summary>
public interface ICityEnricher
{
    Task<Result<CityFacts>> FetchAsync(
        string cityName,
        CountryCode countryCode,
        Coordinates coordinates,
        CancellationToken cancellationToken);
}
