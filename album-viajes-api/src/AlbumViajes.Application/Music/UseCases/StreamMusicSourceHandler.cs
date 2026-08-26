using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>
/// Abre el audio de una fuente para reenviarlo al navegador.
///
/// Con la radio existe porque el album se sirve por https y buena parte del
/// directorio sigue publicando streams en http plano, que el navegador
/// bloquearia por contenido mixto. Con las canciones, porque iTunes las entrega
/// con un tipo de contenido que no todos los navegadores reconocen, y porque asi
/// quien solo viene a mirar el album no le deja su direccion a Apple.
/// </summary>
public sealed class StreamMusicSourceHandler(ICityRepository cities, IAudioStreamReader reader)
{
    /// <summary>
    /// Las muestras de iTunes llegan como <c>audio/x-m4p</c>, que algunos
    /// navegadores rechazan aunque el fichero sea AAC corriente.
    /// </summary>
    private const string TrackContentType = "audio/mp4";

    public async Task<Result<AudioFeed>> HandleAsync(
        Guid cityId,
        Guid musicSourceId,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var source = city.MusicSources.FirstOrDefault(candidate => candidate.Id == musicSourceId);
        if (source is null)
        {
            return MusicErrors.NotFound(musicSourceId);
        }

        var isRadio = source.Kind == MusicKind.RadioStation;
        var url = isRadio ? source.RadioStreamUrl : source.TrackAudioUrl;

        if (url is null)
        {
            return MusicErrors.NotStreamable();
        }

        // La emisora se reenvia segun llega, porque no termina nunca. La cancion
        // se trae entera: es un fichero, y el navegador tiene que poder saltar
        // dentro de el para reproducirlo.
        var audio = isRadio
            ? await reader.OpenAsync(new Uri(url), cancellationToken)
            : await reader.DownloadAsync(new Uri(url), cancellationToken);

        if (!audio.IsSuccess)
        {
            return MusicErrors.SourceUnreachable(source.Label);
        }

        return isRadio ? audio : audio.Value with { ContentType = TrackContentType };
    }
}
