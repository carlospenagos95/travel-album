using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Cities.UseCases;

/// <summary>
/// Completa la ficha de una ciudad con lo que publican Wikidata y Wikipedia.
/// Si la fuente externa falla, la ciudad se queda como estaba: no se guarda nada
/// a medias.
/// </summary>
public sealed class EnrichCityHandler(
    ICityRepository cities,
    ICityEnricher enricher,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<CityDetailResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(id, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(id);
        }

        var facts = await enricher.FetchAsync(city.Name, city.CountryCode, city.Coordinates, cancellationToken);
        if (!facts.IsSuccess)
        {
            return facts.Error!;
        }

        city.ApplyEnrichment(
            facts.Value.Population,
            facts.Value.PopulationSource,
            facts.Value.ShortDescription,
            facts.Value.WikidataId,
            facts.Value.WikipediaUrl,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return city.ToDetail();
    }
}
