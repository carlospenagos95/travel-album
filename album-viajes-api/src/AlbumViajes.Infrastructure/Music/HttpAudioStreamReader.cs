using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Music;

/// <summary>
/// Lee el audio de una fuente para reenviarlo.
///
/// De una emisora se toma en cuanto llegan las cabeceras: el stream no termina
/// nunca, asi que esperar al final del cuerpo colgaria la peticion. De una
/// cancion se trae el fichero entero, porque servirlo por partes es lo unico
/// que deja al navegador reproducirlo.
/// </summary>
internal sealed class HttpAudioStreamReader(
    HttpClient http,
    IOptions<MusicOptions> options,
    ILogger<HttpAudioStreamReader> logger) : IAudioStreamReader
{
    private const string FallbackContentType = "audio/mpeg";

    private readonly MusicOptions _options = options.Value;

    public async Task<Result<AudioFeed>> OpenAsync(Uri streamUrl, CancellationToken cancellationToken)
    {
        try
        {
            var response = await http.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("La emisora {Url} respondio {Status}.", streamUrl, (int)response.StatusCode);
                response.Dispose();
                return Error.External("music.stationUnreachable", "La emisora no esta transmitiendo ahora mismo.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? FallbackContentType;
            var content = await response.Content.ReadAsStreamAsync(cancellationToken);

            return new AudioFeed(new HttpBoundStream(content, response), contentType);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation(exception, "No se pudo abrir el stream de {Url}.", streamUrl);
            return Error.External("music.sourceUnreachable", "No se pudo conectar con la fuente de musica.");
        }
    }

    public async Task<Result<AudioFeed>> DownloadAsync(Uri fileUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("El catalogo respondio {Status} para {Url}.", (int)response.StatusCode, fileUrl);
                return Error.External("music.sourceUnreachable", "La cancion no esta disponible ahora mismo.");
            }

            // Una muestra ronda el megabyte. El tope protege la memoria del
            // servidor de una direccion que apunte a otra cosa.
            var maxBytes = _options.MaxTrackBytes;
            if (response.Content.Headers.ContentLength > maxBytes)
            {
                logger.LogInformation("El audio de {Url} supera el tope de {MaxBytes} bytes.", fileUrl, maxBytes);
                return Error.External("music.sourceUnreachable", "La cancion es demasiado grande para reenviarla.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? FallbackContentType;

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);

            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);

            if (buffer.Length > maxBytes)
            {
                // Sin Content-Length no hay forma de saberlo hasta terminar.
                await buffer.DisposeAsync();
                logger.LogInformation("El audio de {Url} supera el tope de {MaxBytes} bytes.", fileUrl, maxBytes);
                return Error.External("music.sourceUnreachable", "La cancion es demasiado grande para reenviarla.");
            }

            buffer.Position = 0;

            return new AudioFeed(buffer, contentType, SupportsRange: true);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation(exception, "No se pudo traer el audio de {Url}.", fileUrl);
            return Error.External("music.sourceUnreachable", "No se pudo conectar con el catalogo de canciones.");
        }
    }
}

/// <summary>
/// Ata el ciclo de vida de la respuesta HTTP al del stream que se devuelve: quien
/// consume el audio no tiene por que saber que detras hay una peticion abierta,
/// pero cerrarlo debe liberarla.
/// </summary>
internal sealed class HttpBoundStream(Stream inner, HttpResponseMessage response) : Stream
{
    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        inner.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        response.Dispose();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
            response.Dispose();
        }

        base.Dispose(disposing);
    }
}
