using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Application.Abstractions;

public interface ICityRepository
{
    Task<IReadOnlyList<City>> ListAsync(CancellationToken cancellationToken);

    Task<City?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, CountryCode countryCode, Guid? excludingId, CancellationToken cancellationToken);

    Task AddAsync(City city, CancellationToken cancellationToken);

    void Remove(City city);
}
