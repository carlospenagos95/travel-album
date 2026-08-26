using AlbumViajes.Api.Auth;
using AlbumViajes.Api.Http;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Application.Music.UseCases;

namespace AlbumViajes.Api.Endpoints;

internal static class MusicEndpoints
{
    private const string MusicRoute = "/api/cities/{cityId:guid}/music";

    public static IEndpointRouteBuilder MapMusicEndpoints(this IEndpointRouteBuilder app)
    {
        MapDirectory(app);
        MapCityMusic(app);

        return app;
    }

    private static void MapDirectory(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stations/search", async (
                string query,
                string? countryCode,
                SearchRadioStationsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, countryCode, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithTags("Music")
            .WithName("SearchRadioStations")
            .WithSummary("Busca emisoras en el directorio para asociarlas a una ciudad.");

        app.MapGet("/api/tracks/search", async (
                string query,
                SearchTracksHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithTags("Music")
            .WithName("SearchTracks")
            .WithSummary("Busca canciones en el catalogo para asociarlas a una ciudad.");
    }

    private static void MapCityMusic(IEndpointRouteBuilder app)
    {
        // Dos grupos sobre la misma ruta: las convenciones de un grupo alcanzan a
        // todos sus endpoints, tambien a los declarados antes, asi que exigir
        // autenticacion sobre el grupo publico dejaria el album mudo para quien
        // solo viene a mirar.
        var music = app.MapGroup(MusicRoute).WithTags("Music");
        var owned = app.MapGroup(MusicRoute).WithTags("Music").RequireAuthorization(OwnerAuthentication.OwnerPolicy);

        // Publico: quien ve el album oye la ciudad. El servidor hace de puente
        // porque muchas emisoras siguen publicando el stream en http plano y
        // porque iTunes marca sus muestras con un tipo que no todos reconocen.
        music.MapGet("/{musicSourceId:guid}/stream", async (
                Guid cityId,
                Guid musicSourceId,
                StreamMusicSourceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, musicSourceId, cancellationToken);

                // Con rangos activos el navegador puede pedir trozos sueltos, que
                // es lo que necesita para leer el indice de un .m4a. La radio no
                // los admite: su stream no termina y no se puede recorrer.
                return result.ToHttpResult(audio => Results.Stream(
                    audio.Content,
                    audio.ContentType,
                    enableRangeProcessing: audio.SupportsRange));
            })
            .WithName("StreamMusicSource")
            .WithSummary("Reenvia el audio de la fuente de musica asociada a la ciudad.");

        owned.MapPost("/radio", async (
                Guid cityId,
                AddRadioStationRequest request,
                AddRadioStationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, request, cancellationToken);
                return result.ToHttpResult(source => Results.Created($"/api/cities/{cityId}/music/{source.Id}", source));
            })
            .WithName("AddRadioStation")
            .WithSummary("Asocia una emisora de radio a la ciudad.");

        owned.MapPost("/track", async (
                Guid cityId,
                AddTrackRequest request,
                AddTrackHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, request, cancellationToken);
                return result.ToHttpResult(source => Results.Created($"/api/cities/{cityId}/music/{source.Id}", source));
            })
            .WithName("AddTrack")
            .WithSummary("Asocia una cancion del catalogo a la ciudad.");

        owned.MapPut("/{musicSourceId:guid}/default", async (
                Guid cityId,
                Guid musicSourceId,
                SetDefaultMusicSourceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, musicSourceId, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("SetDefaultMusicSource")
            .WithSummary("Elige que fuente suena al abrir la ciudad.");

        owned.MapDelete("/{musicSourceId:guid}", async (
                Guid cityId,
                Guid musicSourceId,
                RemoveMusicSourceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, musicSourceId, cancellationToken);
                return result.ToHttpResult(Results.NoContent);
            })
            .WithName("RemoveMusicSource")
            .WithSummary("Quita una fuente de musica de la ciudad.");
    }
}
