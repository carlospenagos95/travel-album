using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Music;

/// <summary>Errores esperados del area de musica, en un solo lugar.</summary>
public static class MusicErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("music.notFound", $"No existe una fuente de musica con id {id} en esta ciudad.");

    /// <summary>Una fuente sin direccion guardada no hay forma de reproducirla.</summary>
    public static Error NotStreamable() =>
        Error.Validation("music.notStreamable", "Esta fuente de musica no tiene audio que reproducir.");

    public static Error DirectoryUnavailable() =>
        Error.External("music.directoryUnavailable", "El directorio de emisoras no respondio.");

    public static Error SourceUnreachable(string label) =>
        Error.External("music.sourceUnreachable", $"{label} no esta disponible ahora mismo.");

    public static Error LibraryUnavailable() =>
        Error.External("music.libraryUnavailable", "El catalogo de canciones no respondio.");
}
