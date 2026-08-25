using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Cities.UseCases;

public sealed class UpdateCityHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<CityDetailResponse>> HandleAsync(
        Guid id,
        SaveCityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(id, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(id);
        }

        var location = CityLocation.FromRequest(request);
        if (!location.IsSuccess)
        {
            return location.Error!;
        }

        var alreadyExists = await cities.ExistsWithNameAsync(
            request.Name,
            location.Value.CountryCode,
            excludingId: id,
            cancellationToken);

        if (alreadyExists)
        {
            return CityErrors.DuplicateName(request.Name, location.Value.CountryCode);
        }

        var now = clock.UtcNow;

        var renamed = city.Rename(request.Name, request.Country, location.Value.CountryCode, now);
        if (!renamed.IsSuccess)
        {
            return renamed.Error!;
        }

        city.Relocate(location.Value.Coordinates, now);

        var visit = city.RecordVisit(request.VisitedOn, request.Notes, now);
        if (!visit.IsSuccess)
        {
            return visit.Error!;
        }

        var described = city.DescribeManually(request.ShortDescription, request.TypicalFood, now);
        if (!described.IsSuccess)
        {
            return described.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return city.ToDetail();
    }
}
