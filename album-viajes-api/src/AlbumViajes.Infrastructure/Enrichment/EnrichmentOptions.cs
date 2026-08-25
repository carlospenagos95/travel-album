namespace AlbumViajes.Infrastructure.Enrichment;

/// <summary>Ajustes de las fuentes externas, en appsettings bajo "Enrichment".</summary>
public sealed class EnrichmentOptions
{
    public const string SectionName = "Enrichment";

    /// <summary>Idioma de la Wikipedia que se consulta para la descripcion y el enlace.</summary>
    public string WikipediaLanguage { get; set; } = "es";

    /// <summary>
    /// La politica de uso de Wikimedia exige identificarse con un User-Agent
    /// descriptivo y un contacto; sin el, devuelven 403.
    /// </summary>
    public string UserAgent { get; set; } = "AlbumViajes/1.0 (https://github.com/album-viajes)";

    /// <summary>Radio en metros dentro del cual se busca el articulo de la ciudad.</summary>
    public int SearchRadiusMeters { get; set; } = 10_000;

    public int TimeoutSeconds { get; set; } = 15;
}
