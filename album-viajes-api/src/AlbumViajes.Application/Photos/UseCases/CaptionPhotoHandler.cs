using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>Pone o quita el pie de una foto.</summary>
public sealed class CaptionPhotoHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<PhotoResponse>> HandleAsync(
        Guid cityId,
        Guid photoId,
        CaptionPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var captioned = city.CaptionPhoto(photoId, request.Caption, clock.UtcNow);
        if (!captioned.IsSuccess)
        {
            return captioned.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return city.Photos.First(photo => photo.Id == photoId).ToResponse();
    }
}
