using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>
/// Quita la foto del album y borra sus archivos. El orden importa: primero se
/// confirma en base de datos y despues se toca el disco, para que un fallo al
/// guardar no deje la ficha apuntando a un archivo ya borrado.
/// </summary>
public sealed class DeletePhotoHandler(
    ICityRepository cities,
    IPhotoStorage storage,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result> HandleAsync(Guid cityId, Guid photoId, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var removed = city.RemovePhoto(photoId, clock.UtcNow);
        if (!removed.IsSuccess)
        {
            return PhotoErrors.NotFound(photoId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(removed.Value.StoredPath, removed.Value.ThumbnailPath, cancellationToken);

        return Result.Success();
    }
}
