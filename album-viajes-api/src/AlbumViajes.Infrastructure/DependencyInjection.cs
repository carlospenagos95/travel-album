using AlbumViajes.Application.Abstractions;
using AlbumViajes.Infrastructure.Auth;
using AlbumViajes.Infrastructure.Enrichment;
using AlbumViajes.Infrastructure.Music;
using AlbumViajes.Infrastructure.Persistence;
using AlbumViajes.Infrastructure.Persistence.Repositories;
using AlbumViajes.Infrastructure.Photos;
using AlbumViajes.Infrastructure.Storage;
using AlbumViajes.Infrastructure.Time;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlbumViajes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Falta la cadena de conexion 'Database'.");

        services.AddDbContext<AlbumViajesDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AlbumViajesDbContext>());
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IGoogleAccountRepository, GoogleAccountRepository>();
        services.AddSingleton<IClock, SystemClock>();

        // Lo comparten el cache de busquedas de emisoras y el de tokens de Google.
        services.AddMemoryCache();

        AddOwnerAccess(services, configuration);
        AddEnrichment(services, configuration);
        AddPhotos(services, configuration);
        AddMusic(services, configuration);

        return services;
    }

    private static void AddOwnerAccess(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton<IOwnerAllowlist, ConfigurationOwnerAllowlist>();

        // Las claves se guardan en el perfil del usuario del contenedor, que en
        // produccion es un volumen: si se perdieran, cada redespliegue invalidaria
        // las sesiones y los tokens de Google ya guardados.
        services.AddDataProtection().SetApplicationName("AlbumViajes");
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
    }

    /// <summary>
    /// Wikimedia exige un User-Agent que identifique a la aplicacion; sin el
    /// responde 403. El timeout evita que una consulta colgada retenga la
    /// peticion del usuario.
    /// </summary>
    private static void AddEnrichment(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(EnrichmentOptions.SectionName);
        services.Configure<EnrichmentOptions>(section);

        var options = section.Get<EnrichmentOptions>() ?? new EnrichmentOptions();

        services.AddHttpClient<ICityEnricher, WikimediaCityEnricher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });
    }

    private static void AddPhotos(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GooglePhotosOptions.SectionName);
        services.Configure<GooglePhotosOptions>(section);
        services.Configure<PhotoStorageOptions>(configuration.GetSection(PhotoStorageOptions.SectionName));

        var options = section.Get<GooglePhotosOptions>() ?? new GooglePhotosOptions();
        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        services.AddHttpClient<GoogleAccessTokenProvider>(client => client.Timeout = timeout);
        services.AddHttpClient<IPhotoLibrary, GooglePhotosLibrary>(client => client.Timeout = timeout);

        services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
    }

    private static void AddMusic(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MusicOptions.SectionName);
        services.Configure<MusicOptions>(section);

        var options = section.Get<MusicOptions>() ?? new MusicOptions();

        services.AddHttpClient<IRadioDirectory, RadioBrowserDirectory>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.SearchTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        services
            .AddHttpClient<IAudioStreamReader, HttpAudioStreamReader>(client =>
            {
                // Un stream de radio no termina: un timeout global lo cortaria a
                // mitad de cancion. El limite se pone al conectar, no al escuchar.
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(options.StreamTimeoutSeconds),
            });
    }
}
