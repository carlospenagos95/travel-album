using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Cities.UseCases;

public sealed class GetCityHandler(ICityRepository cities)
{
    public async Task<Result<CityDetailResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(id, cancellationToken);
        return city is null ? CityErrors.NotFound(id) : city.ToDetail();
    }
}
