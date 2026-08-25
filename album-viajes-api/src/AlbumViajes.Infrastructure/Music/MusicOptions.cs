namespace AlbumViajes.Infrastructure.Music;

/// <summary>Ajustes del directorio de emisoras, en appsettings bajo "Music".</summary>
public sealed class MusicOptions
{
    public const string SectionName = "Music";

    /// <summary>
    /// El punto de entrada que recomienda Radio Browser: reparte entre sus
    /// servidores en lugar de fijar uno, que puede desaparecer.
    /// </summary>
    public string DirectoryBaseUrl { get; set; } = "https://all.api.radio-browser.info";

    /// <summary>
    /// Radio Browser pide que cada aplicacion se identifique con un User-Agent
    /// propio para poder contactarla si abusa de la API.
    /// </summary>
    public string UserAgent { get; set; } = "AlbumViajes/1.0 (https://github.com/album-viajes)";

    public int SearchTimeoutSeconds { get; set; } = 10;

    /// <summary>Cuanto se reutiliza una busqueda. El catalogo de emisoras cambia despacio.</summary>
    public int SearchCacheMinutes { get; set; } = 30;

    /// <summary>Espera maxima a que la emisora empiece a responder.</summary>
    public int StreamTimeoutSeconds { get; set; } = 20;
}
