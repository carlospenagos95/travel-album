using AlbumViajes.Api.Auth;
using AlbumViajes.Api.Http;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Application.Cities.UseCases;

namespace AlbumViajes.Api.Endpoints;

/// <summary>
/// Los endpoints no contienen logica de negocio: reciben, delegan en un caso de
/// uso y traducen el resultado a HTTP.
///
/// Leer es publico y escribir no: cualquiera con el enlace ve el album, pero
/// solo el duenio lo modifica.
/// </summary>
internal static class CityEndpoints
{
    public static IEndpointRouteBuilder MapCityEndpoints(this IEndpointRouteBuilder app)
    {
        var cities = app.MapGroup("/api/cities").WithTags("Cities");

        cities.MapGet("/", async (ListCitiesHandler handler, CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListCities")
            .WithSummary("Lista las ciudades para pintar el mapa.");

        cities.MapGet("/{id:guid}", async (Guid id, GetCityHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .WithName("GetCity")
            .WithSummary("Devuelve la ficha completa de una ciudad.");

        cities.MapPost("/", async (
                SaveCityRequest request,
                CreateCityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToHttpResult(city => Results.Created($"/api/cities/{city.Id}", city));
            })
            .AddEndpointFilter<ValidationFilter<SaveCityRequest>>()
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithName("CreateCity")
            .WithSummary("Registra una ciudad visitada.");

        cities.MapPut("/{id:guid}", async (
                Guid id,
                SaveCityRequest request,
                UpdateCityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .AddEndpointFilter<ValidationFilter<SaveCityRequest>>()
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithName("UpdateCity")
            .WithSummary("Actualiza una ciudad existente.");

        cities.MapPost("/{id:guid}/enrich", async (
                Guid id,
                EnrichCityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result.ToHttpResult(Results.Ok);
            })
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithName("EnrichCity")
            .WithSummary("Completa la ficha con los datos de Wikidata y Wikipedia.");

        cities.MapDelete("/{id:guid}", async (
                Guid id,
                DeleteCityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result.ToHttpResult(Results.NoContent);
            })
            .RequireAuthorization(OwnerAuthentication.OwnerPolicy)
            .WithName("DeleteCity")
            .WithSummary("Elimina una ciudad.");

        return app;
    }
}
