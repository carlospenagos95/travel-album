using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>
/// Trae al servidor las fotos que el usuario eligio. Se copia el archivo en vez
/// de guardar el enlace porque el que da el selector caduca en una hora: sin la
/// copia, el album se quedaria sin fotos solo.
/// </summary>
public sealed class ImportPickedPhotosHandler(
    ICityRepository cities,
    IPhotoLibrary library,
    IPhotoStorage storage,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<PhotoImportResponse>> HandleAsync(
        Guid cityId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var session = await library.GetPickerSessionAsync(sessionId, cancellationToken);
        if (!session.IsSuccess)
        {
            return session.Error!;
        }

        if (!session.Value.PhotosPicked)
        {
            return PhotoErrors.SelectionPending();
        }

        var picked = await library.ListPickedPhotosAsync(sessionId, cancellationToken);
        if (!picked.IsSuccess)
        {
            return picked.Error!;
        }

        var imported = 0;
        var skipped = 0;

        foreach (var candidate in picked.Value)
        {
            // Volver a elegir una foto que ya esta en el album no es un error:
            // simplemente no se duplica.
            if (city.AlreadyHasPhotoFrom(candidate.MediaId))
            {
                skipped++;
                continue;
            }

            var added = await ImportOneAsync(city, candidate, cancellationToken);
            if (!added.IsSuccess)
            {
                return added.Error!;
            }

            imported++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // La sesion ya no sirve; que no se pueda cerrar no invalida lo importado.
        await library.DiscardPickerSessionAsync(sessionId, cancellationToken);

        return new PhotoImportResponse(imported, skipped, city.Photos.ToGallery());
    }

    private async Task<Result<Photo>> ImportOneAsync(City city, PickedPhoto candidate, CancellationToken cancellationToken)
    {
        var download = await library.DownloadAsync(candidate, cancellationToken);
        if (!download.IsSuccess)
        {
            return download.Error!;
        }

        await using var content = download.Value.Content;

        var stored = await storage.SaveAsync(city.Id, content, download.Value.MimeType, cancellationToken);
        if (!stored.IsSuccess)
        {
            return stored.Error!;
        }

        var added = city.AddPhoto(
            candidate.MediaId,
            candidate.FileName,
            stored.Value.StoredPath,
            stored.Value.ThumbnailPath,
            stored.Value.Width,
            stored.Value.Height,
            candidate.TakenAt,
            clock.UtcNow);

        // El archivo ya esta en disco: si la entidad lo rechaza, no puede quedar huerfano.
        if (!added.IsSuccess)
        {
            await storage.DeleteAsync(stored.Value.StoredPath, stored.Value.ThumbnailPath, cancellationToken);
        }

        return added;
    }
}
