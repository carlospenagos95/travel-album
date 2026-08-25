using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Infrastructure.Photos;

/// <summary>Fallos que solo conoce el adaptador de Google Photos.</summary>
internal static class PhotoLibraryErrors
{
    public static Error NotConfigured() =>
        Error.External(
            "photo.notConfigured",
            "El album no tiene configuradas las credenciales de Google: revisa Auth:Google en la configuracion.");

    public static Error Unavailable() =>
        Error.External("photo.unavailable", "Google Photos no respondio. Intenta de nuevo en un momento.");

    public static Error SessionNotFound() =>
        Error.NotFound("photo.sessionNotFound", "La sesion para elegir fotos caduco. Vuelve a empezar.");

    public static Error TooLarge(string fileName, long maxBytes) =>
        Error.Validation(
            "photo.tooLarge",
            $"{fileName} supera el limite de {maxBytes / (1024 * 1024)} MB por archivo.");
}
