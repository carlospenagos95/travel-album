using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>Asocia a la ciudad una cancion elegida en el catalogo.</summary>
public sealed class AddTrackHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<MusicSourceResponse>> HandleAsync(
        Guid cityId,
        AddTrackRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var added = city.AddTrack(request.TrackId, request.Title, request.Artist, request.AudioUrl, clock.UtcNow);
        if (!added.IsSuccess)
        {
            return added.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return added.Value.ToResponse();
    }
}
