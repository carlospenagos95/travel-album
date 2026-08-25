using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Music;

/// <summary>Errores esperados del area de musica, en un solo lugar.</summary>
public static class MusicErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("music.notFound", $"No existe una fuente de musica con id {id} en esta ciudad.");

    /// <summary>Los videos de YouTube los reproduce el navegador; el proxy es solo para la radio.</summary>
    public static Error NotStreamable() =>
        Error.Validation("music.notStreamable", "Solo las emisoras de radio se reproducen desde el servidor.");

    public static Error DirectoryUnavailable() =>
        Error.External("music.directoryUnavailable", "El directorio de emisoras no respondio.");

    public static Error StationUnreachable(string label) =>
        Error.External("music.stationUnreachable", $"La emisora {label} no esta transmitiendo ahora mismo.");
}
