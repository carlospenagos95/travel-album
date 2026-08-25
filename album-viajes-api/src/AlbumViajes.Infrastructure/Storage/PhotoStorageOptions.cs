namespace AlbumViajes.Infrastructure.Storage;

/// <summary>Donde viven las fotos copiadas, en appsettings bajo "PhotoStorage".</summary>
public sealed class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    /// <summary>
    /// En el contenedor apunta a un volumen: si las fotos quedaran dentro de la
    /// imagen, cada redespliegue las borraria.
    /// </summary>
    public string RootPath { get; set; } = "/data/photos";

    /// <summary>Ancho de la miniatura de la galeria. La altura sale de la proporcion original.</summary>
    public int ThumbnailWidth { get; set; } = 480;
}
