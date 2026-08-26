using AlbumViajes.Application.Abstractions;
using AlbumViajes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace AlbumViajes.Tests.Integration;

/// <summary>
/// Levanta la API contra un PostgreSQL real en contenedor. Nada de bases en
/// memoria: lo que se prueba aqui es el comportamiento con el motor de verdad,
/// incluidos indices unicos y las traducciones de EF Core.
///
/// Solo se sustituye lo que sale a internet. El almacenamiento de fotos es el de
/// verdad, contra una carpeta temporal, para que la generacion de miniaturas
/// tambien quede probada.
/// </summary>
public sealed class AlbumViajesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Las fuentes externas se controlan desde el test, no desde la red.</summary>
    public StubCityEnricher Enricher { get; } = new();

    public StubPhotoLibrary PhotoLibrary { get; } = new();

    public StubRadioDirectory RadioDirectory { get; } = new();

    public StubMusicLibrary MusicLibrary { get; } = new();

    public StubAudioStreamReader AudioStreamReader { get; } = new();

    /// <summary>Carpeta donde acaban las fotos importadas durante los tests.</summary>
    public string PhotoRoot { get; } = Path.Combine(Path.GetTempPath(), "albumviajes-tests", Guid.NewGuid().ToString("N"));

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("albumviajes")
        .WithUsername("albumviajes")
        .WithPassword("albumviajes")
        .Build();

    // Implementacion explicita: WebApplicationFactory ya define DisposeAsync con
    // otra firma, asi que las dos interfaces conviven sin chocar.
    async Task IAsyncLifetime.InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();

        if (Directory.Exists(PhotoRoot))
        {
            Directory.Delete(PhotoRoot, recursive: true);
        }
    }

    /// <summary>Cliente que la API reconoce como duenio del album.</summary>
    public HttpClient CreateOwnerClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestOwnerAuthenticationHandler.HeaderName, "1");

        return client;
    }

    /// <summary>Deja la base vacia para que cada test parta del mismo estado.</summary>
    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlbumViajesDbContext>();

        // Fotos y musica caen con la ciudad por la cascada de la base de datos.
        await dbContext.Cities.ExecuteDeleteAsync();
        await dbContext.GoogleAccounts.ExecuteDeleteAsync();

        Enricher.Reset();
        PhotoLibrary.Reset();
        RadioDirectory.Reset();
        MusicLibrary.Reset();
        AudioStreamReader.Reset();

        if (Directory.Exists(PhotoRoot))
        {
            Directory.Delete(PhotoRoot, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("PhotoStorage:RootPath", PhotoRoot);
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICityEnricher>();
            services.AddSingleton<ICityEnricher>(Enricher);

            services.RemoveAll<IPhotoLibrary>();
            services.AddSingleton<IPhotoLibrary>(PhotoLibrary);

            services.RemoveAll<IRadioDirectory>();
            services.AddSingleton<IRadioDirectory>(RadioDirectory);

            services.RemoveAll<IMusicLibrary>();
            services.AddSingleton<IMusicLibrary>(MusicLibrary);

            services.RemoveAll<IAudioStreamReader>();
            services.AddSingleton<IAudioStreamReader>(AudioStreamReader);

            // El esquema de prueba pasa a ser el predeterminado, asi que la
            // politica del duenio se resuelve con la cabecera del test en lugar
            // de con la cookie que emitiria Google.
            services
                .AddAuthentication(TestOwnerAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestOwnerAuthenticationHandler>(
                    TestOwnerAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }
}
