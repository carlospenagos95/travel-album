using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>Asocia a la ciudad una emisora elegida en el directorio.</summary>
public sealed class AddRadioStationHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<MusicSourceResponse>> HandleAsync(
        Guid cityId,
        AddRadioStationRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var added = city.AddRadioStation(request.StationUuid, request.StationName, request.StreamUrl, clock.UtcNow);
        if (!added.IsSuccess)
        {
            return added.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return added.Value.ToResponse();
    }
}
