using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Abstractions;

public interface IGoogleAccountRepository
{
    Task<GoogleAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(GoogleAccount account, CancellationToken cancellationToken);
}
