using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>
/// Abre el audio de una emisora para reenviarlo al navegador. Existe porque el
/// album se sirve por https y buena parte del directorio sigue publicando
/// streams en http plano, que el navegador bloquearia por contenido mixto.
/// </summary>
public sealed class StreamRadioStationHandler(ICityRepository cities, IAudioStreamReader reader)
{
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

        if (source.Kind != MusicKind.RadioStation || source.RadioStreamUrl is null)
        {
            return MusicErrors.NotStreamable();
        }

        var audio = await reader.OpenAsync(new Uri(source.RadioStreamUrl), cancellationToken);

        return audio.IsSuccess ? audio : MusicErrors.StationUnreachable(source.Label);
    }
}
