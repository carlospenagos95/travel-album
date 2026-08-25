using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>Elige cual de las fuentes suena al abrir la ciudad.</summary>
public sealed class SetDefaultMusicSourceHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<IReadOnlyList<MusicSourceResponse>>> HandleAsync(
        Guid cityId,
        Guid musicSourceId,
        CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var updated = city.SetDefaultMusicSource(musicSourceId, clock.UtcNow);
        if (!updated.IsSuccess)
        {
            return MusicErrors.NotFound(musicSourceId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<MusicSourceResponse>>.Success(city.MusicSources.ToPlaylist());
    }
}
