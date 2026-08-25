using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>Quita una fuente de musica de la ciudad.</summary>
public sealed class RemoveMusicSourceHandler(ICityRepository cities, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result> HandleAsync(Guid cityId, Guid musicSourceId, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var removed = city.RemoveMusicSource(musicSourceId, clock.UtcNow);
        if (!removed.IsSuccess)
        {
            return MusicErrors.NotFound(musicSourceId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
