using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>Cual de las dos copias de la foto se pide.</summary>
public enum PhotoRendition
{
    Original,
    Thumbnail,
}

/// <summary>
/// Sirve el archivo desde el almacen propio. Es publico: cualquiera con el enlace
/// del album ve las fotos, igual que ve la ficha.
/// </summary>
public sealed class GetPhotoFileHandler(ICityRepository cities, IPhotoStorage storage)
{
    public async Task<Result<StoredFile>> HandleAsync(
        Guid cityId,
        Guid photoId,
        PhotoRendition rendition,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var photo = city.Photos.FirstOrDefault(candidate => candidate.Id == photoId);
        if (photo is null)
        {
            return PhotoErrors.NotFound(photoId);
        }

        var path = rendition == PhotoRendition.Thumbnail ? photo.ThumbnailPath : photo.StoredPath;

        var file = await storage.OpenAsync(path, cancellationToken);

        return file.IsSuccess ? file : PhotoErrors.FileMissing();
    }
}
