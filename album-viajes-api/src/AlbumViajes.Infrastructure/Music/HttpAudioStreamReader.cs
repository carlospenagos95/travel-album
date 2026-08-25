using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace AlbumViajes.Infrastructure.Music;

/// <summary>
/// Abre el audio de una emisora y lo entrega tal cual para reenviarlo.
///
/// La lectura se hace en cuanto llegan las cabeceras: un stream de radio no
/// termina nunca, asi que esperar al final del cuerpo colgaria la peticion.
/// </summary>
internal sealed class HttpAudioStreamReader(HttpClient http, ILogger<HttpAudioStreamReader> logger) : IAudioStreamReader
{
    private const string FallbackContentType = "audio/mpeg";

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
            return Error.External("music.stationUnreachable", "No se pudo conectar con la emisora.");
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
