using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AlbumViajes.Infrastructure.Persistence.Repositories;

internal sealed class CityRepository(AlbumViajesDbContext dbContext) : ICityRepository
{
    /// <summary>
    /// La lista del mapa necesita las fotos para saber cual es la portada y la
    /// musica para marcar que la ciudad suena, asi que se traen de una vez en
    /// lugar de una consulta por ciudad.
    /// </summary>
    public async Task<IReadOnlyList<City>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Cities
            .AsNoTracking()
            .Include(city => city.Photos)
            .Include(city => city.MusicSources)
            .OrderBy(city => city.Name)
            .ToListAsync(cancellationToken);

    public async Task<City?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Cities
            .Include(city => city.Photos)
            .Include(city => city.MusicSources)
            .FirstOrDefaultAsync(city => city.Id == id, cancellationToken);

    public async Task<bool> ExistsWithNameAsync(
        string name,
        CountryCode countryCode,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var normalized = name.Trim();

        return await dbContext.Cities
            .AsNoTracking()
            .Where(city => excludingId == null || city.Id != excludingId)
            .AnyAsync(
                city => EF.Functions.ILike(city.Name, normalized) && city.CountryCode == countryCode,
                cancellationToken);
    }

    public async Task AddAsync(City city, CancellationToken cancellationToken) =>
        await dbContext.Cities.AddAsync(city, cancellationToken);

    public void Remove(City city) => dbContext.Cities.Remove(city);
}
