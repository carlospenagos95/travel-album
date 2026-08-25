using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>Audio en curso de una emisora. Quien lo recibe libera el contenido.</summary>
public sealed record AudioFeed(Stream Content, string ContentType);

/// <summary>
/// Puerto para leer el audio de una emisora. Existe porque buena parte del
/// directorio publica todavia URLs en http plano: el navegador las bloquearia
/// por contenido mixto, asi que el backend las lee y las reenvia sobre https.
/// </summary>
public interface IAudioStreamReader
{
    Task<Result<AudioFeed>> OpenAsync(Uri streamUrl, CancellationToken cancellationToken);
}
