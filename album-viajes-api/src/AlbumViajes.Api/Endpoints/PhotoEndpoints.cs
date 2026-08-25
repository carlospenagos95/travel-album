using AlbumViajes.Api.Auth;
using AlbumViajes.Api.Http;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Application.Photos.UseCases;

namespace AlbumViajes.Api.Endpoints;

/// <summary>
/// Fotos de una ciudad. Las rutas cuelgan de la ciudad porque una foto solo
/// existe dentro de su album: no hay forma de llegar a ella por otro camino.
/// </summary>
internal static class PhotoEndpoints
{
    /// <summary>
    /// Un archivo servido aqui nunca cambia de contenido: su nombre es un GUID y
    /// borrar la foto cambia la URL. Por eso se puede cachear indefinidamente.
    /// </summary>
    private const string ImmutableCache = "public, max-age=31536000, immutable";

    private const string PhotosRoute = "/api/cities/{cityId:guid}/photos";

    public static IEndpointRouteBuilder MapPhotoEndpoints(this IEndpointRouteBuilder app)
    {
        // Dos grupos sobre la misma ruta: las convenciones de un grupo alcanzan a
        // todos sus endpoints, tambien a los declarados antes, asi que exigir
        // autenticacion sobre el grupo publico cerraria tambien la galeria.
        var photos = app.MapGroup(PhotosRoute).WithTags("Photos");
        var owned = app.MapGroup(PhotosRoute).WithTags("Photos").RequireAuthorization(OwnerAuthentication.OwnerPolicy);

        photos.MapGet("/{photoId:guid}/file", (
                Guid cityId,
                Guid photoId,
                HttpContext http,
                GetPhotoFileHandler handler,
                CancellationToken cancellationToken) =>
                ServeAsync(cityId, photoId, PhotoRendition.Original, http, handler, cancellationToken))
            .WithName("GetPhotoFile")
            .WithSummary("Sirve la foto original desde el servidor.");

        photos.MapGet("/{photoId:guid}/thumbnail", (
                Guid cityId,
                Guid photoId,
                HttpContext http,
                GetPhotoFileHandler handler,
                CancellationToken cancellationToken) =>
                ServeAsync(cityId, photoId, PhotoRendition.Thumbnail, http, handler, cancellationToken))
            .WithName("GetPhotoThumbnail")
            .WithSummary("Sirve la miniatura de la foto.");

        owned.MapPost("/picker-session", async (
                Guid cityId,
                StartPhotoPickerSessionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("StartPhotoPickerSession")
            .WithSummary("Abre el selector de Google Photos para esta ciudad.");

        owned.MapGet("/picker-session/{sessionId}", async (
                string sessionId,
                GetPhotoPickerSessionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(sessionId, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("GetPhotoPickerSession")
            .WithSummary("Consulta si el usuario ya termino de elegir fotos.");

        owned.MapPost("/import", async (
                Guid cityId,
                string sessionId,
                ImportPickedPhotosHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, sessionId, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("ImportPickedPhotos")
            .WithSummary("Copia al servidor las fotos elegidas.");

        owned.MapPut("/order", async (
                Guid cityId,
                ReorderPhotosRequest request,
                ReorderPhotosHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, request, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("ReorderPhotos")
            .WithSummary("Fija el orden de la galeria.");

        owned.MapPut("/{photoId:guid}/caption", async (
                Guid cityId,
                Guid photoId,
                CaptionPhotoRequest request,
                CaptionPhotoHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, photoId, request, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("CaptionPhoto")
            .WithSummary("Pone o quita el pie de una foto.");

        owned.MapDelete("/{photoId:guid}", async (
                Guid cityId,
                Guid photoId,
                DeletePhotoHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cityId, photoId, cancellationToken);
                return result.ToHttpResult(Results.NoContent);
            })
            .WithName("DeletePhoto")
            .WithSummary("Quita una foto del album y borra su archivo.");

        return app;
    }

    private static async Task<IResult> ServeAsync(
        Guid cityId,
        Guid photoId,
        PhotoRendition rendition,
        HttpContext http,
        GetPhotoFileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cityId, photoId, rendition, cancellationToken);

        return result.ToHttpResult(file =>
        {
            http.Response.Headers.CacheControl = ImmutableCache;
            return Results.Stream(file.Content, file.ContentType);
        });
    }
}
