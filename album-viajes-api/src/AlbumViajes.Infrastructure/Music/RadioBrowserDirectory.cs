using System.Globalization;
using System.Text.Json;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Music;
using AlbumViajes.Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Music;

/// <summary>
/// Adaptador del directorio Radio Browser.
///
/// Se pide con <c>hidebroken</c> y ordenado por votos porque el catalogo esta
/// lleno de emisoras muertas: sin eso, la primera sugerencia suele no sonar.
/// Las respuestas se cachean para no repetir la misma consulta mientras el
/// usuario teclea.
/// </summary>
internal sealed class RadioBrowserDirectory(
    HttpClient http,
    IMemoryCache cache,
    IOptions<MusicOptions> options,
    ILogger<RadioBrowserDirectory> logger) : IRadioDirectory
{
    private readonly MusicOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<RadioStation>>> SearchAsync(
        string query,
        string? countryCode,
        int limit,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFor(query, countryCode, limit);

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<RadioStation>? cached) && cached is not null)
        {
            return Result<IReadOnlyList<RadioStation>>.Success(cached);
        }

        try
        {
            using var response = await http.GetAsync(BuildUri(query, countryCode, limit), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Radio Browser respondio {Status}.", (int)response.StatusCode);
                return MusicErrors.DirectoryUnavailable();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);

            var stations = ReadStations(document.RootElement);

            cache.Set(cacheKey, stations, TimeSpan.FromMinutes(_options.SearchCacheMinutes));

            return Result<IReadOnlyList<RadioStation>>.Success(stations);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "Fallo la busqueda de emisoras para {Query}.", query);
            return MusicErrors.DirectoryUnavailable();
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Vencio el timeout del HttpClient; no lo cancelo quien llamo.
            logger.LogWarning(exception, "Se agoto el tiempo buscando emisoras para {Query}.", query);
            return MusicErrors.DirectoryUnavailable();
        }
    }

    private static RadioStation[] ReadStations(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return root.EnumerateArray()
            .Select(ReadStation)
            .OfType<RadioStation>()
            .ToArray();
    }

    private static RadioStation? ReadStation(JsonElement element)
    {
        // Sin url resuelta la emisora no se puede reproducir, asi que no se ofrece.
        var streamUrl = Text(element, "url_resolved") ?? Text(element, "url");
        var uuid = Text(element, "stationuuid");
        var name = Text(element, "name");

        if (streamUrl is null || uuid is null || name is null)
        {
            return null;
        }

        return new RadioStation(
            uuid,
            name,
            streamUrl,
            Text(element, "country"),
            Text(element, "countrycode"),
            Text(element, "tags"),
            Number(element, "votes"),
            Text(element, "codec"),
            Number(element, "bitrate"));
    }

    private Uri BuildUri(string query, string? countryCode, int limit)
    {
        var url = $"{_options.DirectoryBaseUrl}/json/stations/search"
            + $"?name={Uri.EscapeDataString(query)}"
            + $"&limit={limit.ToString(CultureInfo.InvariantCulture)}"
            + "&hidebroken=true&order=votes&reverse=true";

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            url += $"&countrycode={Uri.EscapeDataString(countryCode.Trim().ToUpperInvariant())}";
        }

        return new Uri(url);
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() is { Length: > 0 } text ? text : null
            : null;

    private static int Number(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static string CacheKeyFor(string query, string? countryCode, int limit) =>
        string.Create(CultureInfo.InvariantCulture, $"radio-search:{query.ToLowerInvariant()}:{countryCode}:{limit}");
}
