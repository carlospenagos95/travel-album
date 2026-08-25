using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AlbumViajes.Infrastructure.Storage;

/// <summary>
/// Guarda las fotos en el disco del servidor, una carpeta por ciudad.
///
/// El archivo se nombra con un GUID y no con el nombre de origen: el nombre que
/// viene de fuera puede traer rutas, caracteres del sistema o repetirse. El
/// original se conserva byte a byte, sin recodificar, para no perder calidad.
/// </summary>
internal sealed class LocalPhotoStorage(
    IOptions<PhotoStorageOptions> options,
    ILogger<LocalPhotoStorage> logger) : IPhotoStorage
{
    private const string ThumbnailSuffix = "_thumb.jpg";

    private readonly PhotoStorageOptions _options = options.Value;

    public async Task<Result<StoredPhoto>> SaveAsync(
        Guid cityId,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_options.RootPath, cityId.ToString("N"));
        Directory.CreateDirectory(folder);

        var name = Guid.NewGuid().ToString("N");
        var originalRelative = Path.Combine(cityId.ToString("N"), name + ExtensionFor(mimeType));
        var thumbnailRelative = Path.Combine(cityId.ToString("N"), name + ThumbnailSuffix);

        try
        {
            await using (var file = File.Create(Path.Combine(_options.RootPath, originalRelative)))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            content.Position = 0;

            using var image = await Image.LoadAsync(content, cancellationToken);
            await WriteThumbnailAsync(image, Path.Combine(_options.RootPath, thumbnailRelative), cancellationToken);

            return new StoredPhoto(ToRelativeUrl(originalRelative), ToRelativeUrl(thumbnailRelative), image.Width, image.Height);
        }
        catch (Exception exception) when (exception is IOException or UnknownImageFormatException or InvalidImageContentException)
        {
            logger.LogWarning(exception, "No se pudo guardar la foto de la ciudad {City}.", cityId);

            // Sin esto quedarian archivos a medias ocupando el volumen.
            await DeleteAsync(ToRelativeUrl(originalRelative), ToRelativeUrl(thumbnailRelative), cancellationToken);

            return Error.External("photo.storageFailed", "No se pudo guardar la foto en el servidor.");
        }
    }

    public Task<Result<StoredFile>> OpenAsync(string relativePath, CancellationToken cancellationToken)
    {
        var path = Resolve(relativePath);

        if (path is null || !File.Exists(path))
        {
            return Task.FromResult(Result<StoredFile>.Failure(
                Error.NotFound("photo.fileMissing", "El archivo de la foto ya no esta en el servidor.")));
        }

        Stream content = File.OpenRead(path);

        return Task.FromResult(Result<StoredFile>.Success(new StoredFile(content, ContentTypeFor(path))));
    }

    public Task DeleteAsync(string relativePath, string thumbnailRelativePath, CancellationToken cancellationToken)
    {
        Delete(relativePath);
        Delete(thumbnailRelativePath);

        return Task.CompletedTask;
    }

    private async Task WriteThumbnailAsync(Image image, string path, CancellationToken cancellationToken)
    {
        // Una foto ya pequenia no se agranda: solo se recodifica a jpeg.
        var width = Math.Min(_options.ThumbnailWidth, image.Width);

        using var thumbnail = image.Clone(context => context.Resize(new ResizeOptions
        {
            Size = new Size(width, 0),
            Mode = ResizeMode.Max,
        }));

        await thumbnail.SaveAsJpegAsync(path, new JpegEncoder { Quality = 80 }, cancellationToken);
    }

    private void Delete(string relativePath)
    {
        var path = Resolve(relativePath);

        // Que ya no exista es exactamente lo que se buscaba.
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Convierte una ruta relativa guardada en la base a una ruta del sistema, y
    /// se niega a salir de la carpeta raiz: una fila manipulada no debe poder
    /// leer archivos del servidor.
    /// </summary>
    private string? Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var root = Path.GetFullPath(_options.RootPath);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        return candidate.StartsWith(root, StringComparison.Ordinal) ? candidate : null;
    }

    /// <summary>La base guarda siempre barras normales, independientes del sistema operativo.</summary>
    private static string ToRelativeUrl(string relativePath) => relativePath.Replace('\\', '/');

    private static string ExtensionFor(string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/heic" or "image/heif" => ".heic",
        _ => ".jpg",
    };

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".heic" => "image/heic",
        _ => "image/jpeg",
    };
}
