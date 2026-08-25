using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlbumViajes.Infrastructure.Persistence;

/// <summary>
/// Solo la usan las herramientas de EF Core al generar migraciones. Existe para que
/// el proyecto Api no tenga que arrastrar el paquete Design, que no pinta en runtime.
/// La cadena se toma de ALBUMVIAJES_DESIGN_CONNECTION si esta definida.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AlbumViajesDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5433;Database=albumviajes;Username=albumviajes;Password=albumviajes";

    public AlbumViajesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ALBUMVIAJES_DESIGN_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<AlbumViajesDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AlbumViajesDbContext(options);
    }
}
