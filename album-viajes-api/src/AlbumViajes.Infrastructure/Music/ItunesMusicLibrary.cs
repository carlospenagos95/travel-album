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
/// Adaptador del buscador de iTunes: el catalogo comercial completo, sin clave ni
/// registro y acotado por pais.
///
/// Lo que entrega son muestras de treinta segundos, no la cancion entera. Es el
/// limite de cualquier catalogo libre: escuchar completa una cancion comercial
/// obliga a que sea el propio visitante quien se identifique con una cuenta de
/// pago, y este album se ve sin identificarse.
///
/// Las respuestas se cachean para no repetir la misma consulta mientras el
/// usuario teclea, igual que en <see cref="RadioBrowserDirectory"/>. Ese cache y
/// el debounce del buscador son ademas lo que mantiene la busqueda dentro del
/// ritmo que admite Apple, que ronda las veinte peticiones por minuto.
/// </summary>
internal sealed class ItunesMusicLibrary(
    HttpClient http,
    IMemoryCache cache,
    IOptions<MusicOptions> options,
    ILogger<ItunesMusicLibrary> logger) : IMusicLibrary
{
    private const int MillisecondsPerSecond = 1000;

    private readonly MusicOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<LibraryTrack>>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyFor(query, limit);

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<LibraryTrack>? cached) && cached is not null)
        {
            return Result<IReadOnlyList<LibraryTrack>>.Success(cached);
        }

        try
        {
            using var response = await http.GetAsync(BuildUri(query, limit), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("iTunes respondio {Status}.", (int)response.StatusCode);
                return MusicErrors.LibraryUnavailable();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);

            var tracks = ReadTracks(document.RootElement);

            cache.Set(cacheKey, tracks, TimeSpan.FromMinutes(_options.SearchCacheMinutes));

            return Result<IReadOnlyList<LibraryTrack>>.Success(tracks);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "Fallo la busqueda de canciones para {Query}.", query);
            return MusicErrors.LibraryUnavailable();
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Vencio el timeout del HttpClient; no lo cancelo quien llamo.
            logger.LogWarning(exception, "Se agoto el tiempo buscando canciones para {Query}.", query);
            return MusicErrors.LibraryUnavailable();
        }
    }

    private static LibraryTrack[] ReadTracks(JsonElement root)
    {
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return results.EnumerateArray()
            .Select(ReadTrack)
            .OfType<LibraryTrack>()
            .ToArray();
    }

    private static LibraryTrack? ReadTrack(JsonElement element)
    {
        // Sin muestra la cancion no se puede reproducir, asi que no se ofrece.
        var previewUrl = Text(element, "previewUrl");
        var id = Text(element, "trackId");
        var name = Text(element, "trackName");

        if (previewUrl is null || id is null || name is null)
        {
            return null;
        }

        return new LibraryTrack(
            id,
            name,
            Text(element, "artistName"),
            previewUrl,
            // Es la duracion de la cancion entera, no la de la muestra: dato del
            // tema, no de lo que se va a oir.
            Number(element, "trackTimeMillis") / MillisecondsPerSecond,
            Text(element, "artworkUrl100"));
    }

    private Uri BuildUri(string query, int limit)
    {
        var url = $"{_options.LibraryBaseUrl}"
            + $"?term={Uri.EscapeDataString(query)}"
            + "&media=music&entity=song"
            + $"&limit={limit.ToString(CultureInfo.InvariantCulture)}"
            // El pais decide que catalogo se ve y que esta disponible en el.
            + $"&country={Uri.EscapeDataString(_options.LibraryCountry)}";

        return new Uri(url);
    }

    private static string? Text(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        // trackId llega como numero; el resto, como texto.
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };

        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static int Number(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private string CacheKeyFor(string query, int limit) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"itunes-search:{query.ToLowerInvariant()}:{_options.LibraryCountry}:{limit}");
}
