using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;

namespace AlbumViajes.Application.Cities.UseCases;

public sealed class ListCitiesHandler(ICityRepository cities)
{
    public async Task<IReadOnlyList<CitySummaryResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var found = await cities.ListAsync(cancellationToken);
        return [.. found.Select(city => city.ToSummary())];
    }
}
