using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Audio listo para reenviar. Quien lo recibe libera el contenido.
///
/// <paramref name="SupportsRange"/> distingue las dos formas de servirlo: un
/// stream de radio no termina nunca y solo se puede entregar segun llega, pero
/// una cancion es un fichero completo y el navegador necesita poder saltar
/// dentro de el.
/// </summary>
public sealed record AudioFeed(Stream Content, string ContentType, bool SupportsRange = false);

/// <summary>
/// Puerto para leer el audio de una fuente de musica. Existe porque buena parte
/// del directorio de emisoras publica todavia URLs en http plano: el navegador
/// las bloquearia por contenido mixto, asi que el backend las lee y las reenvia
/// sobre https.
/// </summary>
public interface IAudioStreamReader
{
    /// <summary>Abre un stream que no termina, como el de una emisora.</summary>
    Task<Result<AudioFeed>> OpenAsync(Uri streamUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Trae entero un fichero de audio para poder servirlo por partes.
    ///
    /// Un .m4a lleva su indice al final: sin poder saltar hasta el, el navegador
    /// no sabe como decodificar lo que recibe y no reproduce nada. Reenviarlo
    /// segun llega, como se hace con la radio, no sirve aqui.
    /// </summary>
    Task<Result<AudioFeed>> DownloadAsync(Uri fileUrl, CancellationToken cancellationToken);
}
