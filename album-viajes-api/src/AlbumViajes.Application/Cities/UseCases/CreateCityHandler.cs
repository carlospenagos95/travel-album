using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Cities.UseCases;

public sealed class CreateCityHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<CityDetailResponse>> HandleAsync(SaveCityRequest request, CancellationToken cancellationToken)
    {
        var location = CityLocation.FromRequest(request);
        if (!location.IsSuccess)
        {
            return location.Error!;
        }

        var alreadyExists = await cities.ExistsWithNameAsync(
            request.Name,
            location.Value.CountryCode,
            excludingId: null,
            cancellationToken);

        if (alreadyExists)
        {
            return CityErrors.DuplicateName(request.Name, location.Value.CountryCode);
        }

        var now = clock.UtcNow;
        var created = City.Create(request.Name, request.Country, location.Value.CountryCode, location.Value.Coordinates, now);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var city = created.Value;

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

        await cities.AddAsync(city, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return city.ToDetail();
    }
}
