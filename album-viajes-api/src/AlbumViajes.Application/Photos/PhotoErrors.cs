using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Photos;

/// <summary>Errores esperados del area de fotos, en un solo lugar.</summary>
public static class PhotoErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("photo.notFound", $"No existe una foto con id {id} en esta ciudad.");

    public static Error SelectionPending() =>
        Error.Validation("photo.selectionPending", "Todavia no has terminado de elegir fotos en Google Photos.");

    public static Error FileMissing() =>
        Error.NotFound("photo.fileMissing", "El archivo de la foto ya no esta en el servidor.");
}
