using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlbumViajes.Infrastructure.Persistence.Repositories;

internal sealed class GoogleAccountRepository(AlbumViajesDbContext dbContext) : IGoogleAccountRepository
{
    public async Task<GoogleAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await dbContext.GoogleAccounts.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);

    public async Task AddAsync(GoogleAccount account, CancellationToken cancellationToken) =>
        await dbContext.GoogleAccounts.AddAsync(account, cancellationToken);
}
