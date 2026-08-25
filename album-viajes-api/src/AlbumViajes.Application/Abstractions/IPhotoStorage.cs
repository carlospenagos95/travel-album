using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>Rutas y dimensiones de una foto ya guardada, relativas al almacen.</summary>
public sealed record StoredPhoto(string StoredPath, string ThumbnailPath, int Width, int Height);

/// <summary>Archivo listo para servirse. Quien lo recibe libera el stream.</summary>
public sealed record StoredFile(Stream Content, string ContentType);

/// <summary>
/// Puerto de almacenamiento de archivos. La aplicacion maneja rutas relativas y
/// nunca construye rutas del sistema de archivos.
/// </summary>
public interface IPhotoStorage
{
    Task<Result<StoredPhoto>> SaveAsync(Guid cityId, Stream content, string mimeType, CancellationToken cancellationToken);

    Task<Result<StoredFile>> OpenAsync(string relativePath, CancellationToken cancellationToken);

    /// <summary>Borra original y miniatura. Que ya no existan no es un error.</summary>
    Task DeleteAsync(string relativePath, string thumbnailRelativePath, CancellationToken cancellationToken);
}
