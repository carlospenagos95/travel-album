using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlbumViajes.Infrastructure.Persistence;

public sealed class AlbumViajesDbContext(DbContextOptions<AlbumViajesDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<City> Cities => Set<City>();

    public DbSet<GoogleAccount> GoogleAccounts => Set<GoogleAccount>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlbumViajesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
