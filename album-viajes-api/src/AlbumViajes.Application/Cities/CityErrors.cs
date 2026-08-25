using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Application.Cities;

/// <summary>Errores esperados del area de ciudades, en un solo lugar.</summary>
public static class CityErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("city.notFound", $"No existe una ciudad con id {id}.");

    public static Error DuplicateName(string name, CountryCode countryCode) =>
        Error.Conflict("city.duplicate", $"Ya registraste {name.Trim()} ({countryCode.Value}).");

    public static Error EnrichmentUnavailable(string name) =>
        Error.External("city.enrichmentUnavailable", $"No se pudieron consultar los datos externos de {name.Trim()}.");

    public static Error EnrichmentNotFound(string name) =>
        Error.NotFound("city.enrichmentNotFound", $"Wikipedia no tiene una entrada que corresponda a {name.Trim()}.");
}
