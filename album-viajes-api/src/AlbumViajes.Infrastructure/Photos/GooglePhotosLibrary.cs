using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Photos;

/// <summary>
/// Adaptador de la Picker API de Google Photos.
///
/// Desde marzo de 2025 una aplicacion de terceros no puede listar la biblioteca
/// del usuario: la unica via es abrir una sesion, dejar que elija en la interfaz
/// de Google y leer despues lo elegido. Los enlaces que devuelve caducan en torno
/// a una hora, asi que aqui solo se descargan; guardarlos no serviria de nada.
/// </summary>
internal sealed class GooglePhotosLibrary(
    HttpClient http,
    GoogleAccessTokenProvider tokens,
    IOptions<GooglePhotosOptions> options,
    ILogger<GooglePhotosLibrary> logger) : IPhotoLibrary
{
    private const int PageSize = 100;

    private readonly GooglePhotosOptions _options = options.Value;

    public async Task<Result<PhotoPickerSession>> StartPickerSessionAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{_options.PickerBaseUrl}/sessions"))
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };

        var document = await SendAsync(request, cancellationToken);
        if (!document.IsSuccess)
        {
            return document.Error!;
        }

        using var json = document.Value;
        return ReadSession(json.RootElement);
    }

    public async Task<Result<PhotoPickerSession>> GetPickerSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, SessionUri(sessionId));

        var document = await SendAsync(request, cancellationToken);
        if (!document.IsSuccess)
        {
            return document.Error!;
        }

        using var json = document.Value;
        return ReadSession(json.RootElement);
    }

    public async Task<Result<IReadOnlyList<PickedPhoto>>> ListPickedPhotosAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var photos = new List<PickedPhoto>();
        string? pageToken = null;

        do
        {
            var uri = new Uri(
                $"{_options.PickerBaseUrl}/mediaItems?sessionId={Uri.EscapeDataString(sessionId)}"
                + $"&pageSize={PageSize.ToString(CultureInfo.InvariantCulture)}"
                + (pageToken is null ? string.Empty : $"&pageToken={Uri.EscapeDataString(pageToken)}"));

            var document = await SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken);
            if (!document.IsSuccess)
            {
                return document.Error!;
            }

            using var json = document.Value;
            var root = json.RootElement;

            if (root.TryGetProperty("mediaItems", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                photos.AddRange(items.EnumerateArray().Select(ReadPickedPhoto).OfType<PickedPhoto>());
            }

            pageToken = root.TryGetProperty("nextPageToken", out var next) ? next.GetString() : null;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return Result<IReadOnlyList<PickedPhoto>>.Success(photos);
    }

    public async Task<Result<PhotoContent>> DownloadAsync(PickedPhoto photo, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAsync(cancellationToken);
        if (!token.IsSuccess)
        {
            return token.Error!;
        }

        // El sufijo "=d" pide el archivo original en lugar de una vista previa.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{photo.SourceReference}=d"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google devolvio {Status} al descargar {File}.", (int)response.StatusCode, photo.FileName);
                return PhotoLibraryErrors.Unavailable();
            }

            if (response.Content.Headers.ContentLength > _options.MaxPhotoBytes)
            {
                return PhotoLibraryErrors.TooLarge(photo.FileName, _options.MaxPhotoBytes);
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);

            var buffered = await BufferAsync(source, cancellationToken);
            if (buffered is null)
            {
                return PhotoLibraryErrors.TooLarge(photo.FileName, _options.MaxPhotoBytes);
            }

            return new PhotoContent(buffered, photo.MimeType);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Fallo la descarga de {File} desde Google Photos.", photo.FileName);
            return PhotoLibraryErrors.Unavailable();
        }
    }

    public async Task DiscardPickerSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var closed = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, SessionUri(sessionId)), cancellationToken);

        if (closed.IsSuccess)
        {
            closed.Value.Dispose();
            return;
        }

        // Cerrarla es cortesia con Google; la sesion caduca sola de todos modos.
        logger.LogInformation("No se pudo cerrar la sesion {Session} del selector.", sessionId);
    }

    /// <summary>
    /// Copia a memoria con tope, en vez de dejar abierto el stream de red: el
    /// archivo todavia pasa por un redimensionado y por el disco, y sostener la
    /// conexion con Google mientras tanto solo la expone a cortarse.
    /// Devuelve null si el archivo se pasa del limite.
    /// </summary>
    private async Task<Stream?> BufferAsync(Stream source, CancellationToken cancellationToken)
    {
        var destination = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;

        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;

            if (total > _options.MaxPhotoBytes)
            {
                await destination.DisposeAsync();
                return null;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        destination.Position = 0;
        return destination;
    }

    private async Task<Result<JsonDocument>> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            var token = await tokens.GetAsync(cancellationToken);
            if (!token.IsSuccess)
            {
                return token.Error!;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            try
            {
                using var response = await http.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return PhotoLibraryErrors.SessionNotFound();
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Google Photos respondio {Status} a {Method} {Path}.",
                        (int)response.StatusCode,
                        request.Method,
                        request.RequestUri?.AbsolutePath);

                    return PhotoLibraryErrors.Unavailable();
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                // Un DELETE correcto puede venir sin cuerpo.
                if (stream.CanSeek && stream.Length == 0)
                {
                    return JsonDocument.Parse("{}");
                }

                return await JsonDocument.ParseAsync(stream, default, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException)
            {
                logger.LogWarning(exception, "Fallo la llamada a Google Photos.");
                return PhotoLibraryErrors.Unavailable();
            }
        }
    }

    private Uri SessionUri(string sessionId) =>
        new($"{_options.PickerBaseUrl}/sessions/{Uri.EscapeDataString(sessionId)}");

    private PhotoPickerSession ReadSession(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var sessionId) ? sessionId.GetString() : null;
        var pickerUri = root.TryGetProperty("pickerUri", out var uri) ? uri.GetString() : null;
        var picked = root.TryGetProperty("mediaItemsSet", out var set) && set.ValueKind == JsonValueKind.True;

        return new PhotoPickerSession(
            id ?? string.Empty,
            pickerUri ?? string.Empty,
            picked,
            ReadPollInterval(root));
    }

    /// <summary>
    /// Google indica cada cuanto consultar en formato de duracion ("5s"). Se
    /// respeta su ritmo en vez de imponer uno propio.
    /// </summary>
    private int ReadPollInterval(JsonElement root)
    {
        if (!root.TryGetProperty("pollingConfig", out var polling)
            || !polling.TryGetProperty("pollInterval", out var interval)
            || interval.GetString() is not { Length: > 1 } text)
        {
            return _options.DefaultPollIntervalSeconds;
        }

        return double.TryParse(
            text.TrimEnd('s'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var seconds)
            ? Math.Max(1, (int)Math.Ceiling(seconds))
            : _options.DefaultPollIntervalSeconds;
    }

    private static PickedPhoto? ReadPickedPhoto(JsonElement item)
    {
        // Un album de viajes guarda fotos; los videos que aparezcan en la seleccion se ignoran.
        if (!item.TryGetProperty("type", out var type)
            || !string.Equals(type.GetString(), "PHOTO", StringComparison.OrdinalIgnoreCase)
            || !item.TryGetProperty("id", out var id)
            || !item.TryGetProperty("mediaFile", out var file)
            || !file.TryGetProperty("baseUrl", out var baseUrl))
        {
            return null;
        }

        var metadata = file.TryGetProperty("mediaFileMetadata", out var meta) ? meta : default;

        return new PickedPhoto(
            id.GetString() ?? string.Empty,
            file.TryGetProperty("filename", out var name) ? name.GetString() ?? "foto.jpg" : "foto.jpg",
            file.TryGetProperty("mimeType", out var mime) ? mime.GetString() ?? "image/jpeg" : "image/jpeg",
            ReadDimension(metadata, "width"),
            ReadDimension(metadata, "height"),
            ReadCreateTime(item),
            baseUrl.GetString() ?? string.Empty);
    }

    private static int ReadDimension(JsonElement metadata, string name) =>
        metadata.ValueKind == JsonValueKind.Object
        && metadata.TryGetProperty(name, out var value)
        && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static DateTimeOffset? ReadCreateTime(JsonElement item) =>
        item.TryGetProperty("createTime", out var createTime)
        && DateTimeOffset.TryParse(
            createTime.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
}
