namespace AlbumViajes.Infrastructure.Music;

/// <summary>Ajustes del directorio de emisoras y del catalogo de canciones, en appsettings bajo "Music".</summary>
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

    /// <summary>
    /// Buscador de iTunes: el catalogo comercial completo, sin clave ni registro.
    /// Lo que entrega son muestras de treinta segundos.
    /// </summary>
    public string LibraryBaseUrl { get; set; } = "https://itunes.apple.com/search";

    /// <summary>
    /// Pais del catalogo, en ISO 3166-1 alfa-2. Decide que canciones se ven y
    /// cuales estan disponibles: el album es colombiano, asi que busca en CO.
    /// </summary>
    public string LibraryCountry { get; set; } = "CO";

    public int SearchTimeoutSeconds { get; set; } = 10;

    /// <summary>Cuanto se reutiliza una busqueda. El catalogo de emisoras cambia despacio.</summary>
    public int SearchCacheMinutes { get; set; } = 30;

    /// <summary>Espera maxima a que la emisora empiece a responder.</summary>
    public int StreamTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Tope de lo que se reenvia de una cancion. Las muestras rondan el
    /// megabyte; el limite protege la memoria del servidor.
    /// </summary>
    public long MaxTrackBytes { get; set; } = 20 * 1024 * 1024;
}
