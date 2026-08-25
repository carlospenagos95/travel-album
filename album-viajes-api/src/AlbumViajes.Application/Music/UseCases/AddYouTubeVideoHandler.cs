using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>Asocia a la ciudad un video de YouTube pegando su enlace.</summary>
public sealed class AddYouTubeVideoHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<MusicSourceResponse>> HandleAsync(
        Guid cityId,
        AddYouTubeVideoRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var added = city.AddYouTubeVideo(request.UrlOrId, request.Title, clock.UtcNow);
        if (!added.IsSuccess)
        {
            return added.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return added.Value.ToResponse();
    }
}
