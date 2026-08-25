using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>Fija el orden de la galeria. La primera foto es la portada de la ciudad.</summary>
public sealed class ReorderPhotosHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<IReadOnlyList<PhotoResponse>>> HandleAsync(
        Guid cityId,
        ReorderPhotosRequest request,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var reordered = city.ReorderPhotos(request.PhotoIds, clock.UtcNow);
        if (!reordered.IsSuccess)
        {
            return reordered.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<PhotoResponse>>.Success(city.Photos.ToGallery());
    }
}
