using System.Globalization;
using System.Text;
using System.Text.Json;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Enrichment;

/// <summary>
/// Trae los datos de la ciudad de Wikipedia y Wikidata en tres pasos: se busca
/// el articulo mas cercano a las coordenadas, se leen su resumen y su
/// identificador de Wikidata, y de Wikidata se saca la poblacion.
///
/// Se parte de las coordenadas y no del nombre porque los nombres se repiten
/// entre paises; un punto en el mapa no.
/// </summary>
public sealed class WikimediaCityEnricher(
    HttpClient http,
    IOptions<EnrichmentOptions> options,
    ILogger<WikimediaCityEnricher> logger) : ICityEnricher
{
    private const string WikidataApiUrl = "https://www.wikidata.org/w/api.php";

    private readonly EnrichmentOptions _options = options.Value;

    public async Task<Result<CityFacts>> FetchAsync(
        string cityName,
        CountryCode countryCode,
        Coordinates coordinates,
        CancellationToken cancellationToken)
    {
        try
        {
            var article = await FindNearestArticleAsync(cityName, coordinates, cancellationToken);
            if (article is null)
            {
                return CityErrors.EnrichmentNotFound(cityName);
            }

            var page = await ReadArticleAsync(article.Value, cancellationToken);
            if (page is null)
            {
                return CityErrors.EnrichmentNotFound(cityName);
            }

            (long? Value, string? Source) population = (null, null);

            if (page.Value.WikidataId is not null)
            {
                population = await ReadPopulationAsync(page.Value.WikidataId, cancellationToken);
            }

            return new CityFacts(
                population.Value,
                population.Source,
                page.Value.Summary,
                page.Value.WikidataId,
                page.Value.Url);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "Fallo la consulta de datos externos de {City}.", cityName);
            return CityErrors.EnrichmentUnavailable(cityName);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Vencio el timeout del HttpClient; no lo cancelo quien llamo.
            logger.LogWarning(exception, "Se agoto el tiempo consultando los datos externos de {City}.", cityName);
            return CityErrors.EnrichmentUnavailable(cityName);
        }
    }

    /// <summary>
    /// Busca articulos alrededor del punto. La API los devuelve ordenados por
    /// distancia; si alguno se titula igual que la ciudad, ese le gana al mas cercano.
    /// </summary>
    private async Task<int?> FindNearestArticleAsync(
        string cityName,
        Coordinates coordinates,
        CancellationToken cancellationToken)
    {
        var query = new (string Key, string Value)[]
        {
            ("action", "query"),
            ("format", "json"),
            ("formatversion", "2"),
            ("list", "geosearch"),
            ("gscoord", FormattableString.Invariant($"{coordinates.Latitude}|{coordinates.Longitude}")),
            ("gsradius", _options.SearchRadiusMeters.ToString(CultureInfo.InvariantCulture)),
            ("gslimit", "10"),
        };

        using var document = await GetJsonAsync(WikipediaApiUrl(), query, cancellationToken);

        if (!document.RootElement.TryGetProperty("query", out var root)
            || !root.TryGetProperty("geosearch", out var results)
            || results.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        int? nearest = null;

        foreach (var result in results.EnumerateArray())
        {
            if (!result.TryGetProperty("pageid", out var pageId)
                || !result.TryGetProperty("title", out var title))
            {
                continue;
            }

            nearest ??= pageId.GetInt32();

            if (LooksLikeSameCity(title.GetString(), cityName))
            {
                return pageId.GetInt32();
            }
        }

        return nearest;
    }

    private async Task<(string? Summary, string? Url, string? WikidataId)?> ReadArticleAsync(
        int pageId,
        CancellationToken cancellationToken)
    {
        var query = new (string Key, string Value)[]
        {
            ("action", "query"),
            ("format", "json"),
            ("formatversion", "2"),
            ("pageids", pageId.ToString(CultureInfo.InvariantCulture)),
            ("prop", "extracts|info|pageprops"),
            ("ppprop", "wikibase_item"),
            ("inprop", "url"),
            ("exintro", "1"),
            ("explaintext", "1"),
        };

        using var document = await GetJsonAsync(WikipediaApiUrl(), query, cancellationToken);

        if (!document.RootElement.TryGetProperty("query", out var root)
            || !root.TryGetProperty("pages", out var pages)
            || pages.ValueKind != JsonValueKind.Array
            || pages.GetArrayLength() == 0)
        {
            return null;
        }

        var page = pages[0];

        var summary = Truncate(
            page.TryGetProperty("extract", out var extract) ? extract.GetString() : null,
            City.MaxShortDescriptionLength);

        var url = page.TryGetProperty("fullurl", out var fullUrl) ? fullUrl.GetString() : null;

        var wikidataId = page.TryGetProperty("pageprops", out var props)
            && props.TryGetProperty("wikibase_item", out var item)
                ? item.GetString()
                : null;

        return (summary, url, wikidataId);
    }

    /// <summary>
    /// Lee la propiedad P1082 (poblacion) de Wikidata. Una ciudad suele acumular
    /// varios censos: se toma el mas reciente y su ano viaja en la procedencia,
    /// para que la ficha no muestre una cifra sin contexto.
    /// </summary>
    private async Task<(long? Value, string? Source)> ReadPopulationAsync(
        string wikidataId,
        CancellationToken cancellationToken)
    {
        var query = new (string Key, string Value)[]
        {
            ("action", "wbgetentities"),
            ("format", "json"),
            ("formatversion", "2"),
            ("ids", wikidataId),
            ("props", "claims"),
        };

        using var document = await GetJsonAsync(WikidataApiUrl, query, cancellationToken);

        if (!document.RootElement.TryGetProperty("entities", out var entities)
            || !entities.TryGetProperty(wikidataId, out var entity)
            || !entity.TryGetProperty("claims", out var claims)
            || !claims.TryGetProperty("P1082", out var populationClaims)
            || populationClaims.ValueKind != JsonValueKind.Array)
        {
            return (null, null);
        }

        long? best = null;
        int? bestYear = null;

        foreach (var claim in populationClaims.EnumerateArray())
        {
            var amount = ReadPopulationAmount(claim);
            if (amount is null)
            {
                continue;
            }

            var year = ReadPointInTimeYear(claim);

            // Un dato sin fecha no se puede comparar: solo sirve si no hay otro.
            if (best is null || (year is not null && (bestYear is null || year > bestYear)))
            {
                best = amount;
                bestYear = year;
            }
        }

        if (best is null)
        {
            return (null, null);
        }

        var source = bestYear is null ? $"Wikidata ({wikidataId})" : $"Wikidata ({wikidataId}), {bestYear}";

        return (best, source);
    }

    private static long? ReadPopulationAmount(JsonElement claim)
    {
        if (!claim.TryGetProperty("mainsnak", out var snak)
            || !snak.TryGetProperty("datavalue", out var datavalue)
            || !datavalue.TryGetProperty("value", out var value)
            || !value.TryGetProperty("amount", out var amount))
        {
            return null;
        }

        // Wikidata escribe las cantidades con signo explicito: "+2427129".
        var text = amount.GetString()?.TrimStart('+');

        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static int? ReadPointInTimeYear(JsonElement claim)
    {
        if (!claim.TryGetProperty("qualifiers", out var qualifiers)
            || !qualifiers.TryGetProperty("P585", out var pointsInTime)
            || pointsInTime.ValueKind != JsonValueKind.Array
            || pointsInTime.GetArrayLength() == 0
            || !pointsInTime[0].TryGetProperty("datavalue", out var datavalue)
            || !datavalue.TryGetProperty("value", out var value)
            || !value.TryGetProperty("time", out var time))
        {
            return null;
        }

        // Formato "+2018-01-01T00:00:00Z": el ano son los cuatro digitos tras el signo.
        var text = time.GetString();

        return text is { Length: >= 5 }
            && int.TryParse(text.AsSpan(1, 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                ? year
                : null;
    }

    private async Task<JsonDocument> GetJsonAsync(
        string url,
        (string Key, string Value)[] query,
        CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(BuildUri(url, query), cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, default, cancellationToken);
    }

    private static Uri BuildUri(string url, (string Key, string Value)[] query)
    {
        var builder = new StringBuilder(url);
        var separator = '?';

        foreach (var (key, value) in query)
        {
            builder.Append(separator).Append(key).Append('=').Append(Uri.EscapeDataString(value));
            separator = '&';
        }

        return new Uri(builder.ToString());
    }

    private string WikipediaApiUrl() => $"https://{_options.WikipediaLanguage}.wikipedia.org/w/api.php";

    /// <summary>
    /// Los titulos traen desambiguadores entre parentesis ("Cartagena (Colombia)"),
    /// asi que se compara solo la parte previa.
    /// </summary>
    private static bool LooksLikeSameCity(string? title, string cityName)
    {
        if (title is null)
        {
            return false;
        }

        var withoutQualifier = title.Split('(')[0].Trim();
        return string.Equals(withoutQualifier, cityName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
