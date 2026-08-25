namespace AlbumViajes.Infrastructure.Photos;

/// <summary>Ajustes del acceso a Google Photos, en appsettings bajo "GooglePhotos".</summary>
public sealed class GooglePhotosOptions
{
    public const string SectionName = "GooglePhotos";

    /// <summary>
    /// El unico permiso que pide la aplicacion: leer lo que el usuario elija en
    /// el selector. Desde marzo de 2025 Google no deja a terceros recorrer la
    /// biblioteca completa, y esta API es la via soportada.
    /// </summary>
    public const string Scope = "https://www.googleapis.com/auth/photospicker.mediaitems.readonly";

    public string PickerBaseUrl { get; set; } = "https://photospicker.googleapis.com/v1";

    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";

    /// <summary>Descargar un original grande tarda mas que una llamada normal.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Tope por foto. Protege el disco del servidor de un video o un RAW enorme.</summary>
    public long MaxPhotoBytes { get; set; } = 30 * 1024 * 1024;

    /// <summary>Ritmo minimo de consulta si Google no indica otro en la sesion.</summary>
    public int DefaultPollIntervalSeconds { get; set; } = 5;
}
