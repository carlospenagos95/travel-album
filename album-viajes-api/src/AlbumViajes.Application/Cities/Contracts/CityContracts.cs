using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Cities.Contracts;

/// <summary>Datos de entrada para crear o actualizar una ciudad.</summary>
public sealed record SaveCityRequest(
    string Name,
    string Country,
    string CountryCode,
    double Latitude,
    double Longitude,
    DateOnly? VisitedOn,
    string? Notes,
    string? ShortDescription,
    string? TypicalFood);

/// <summary>Proyeccion ligera para pintar los pines del mapa.</summary>
public sealed record CitySummaryResponse(
    Guid Id,
    string Name,
    string Country,
    string CountryCode,
    double Latitude,
    double Longitude,
    DateOnly? VisitedOn,
    string? CoverThumbnailUrl,
    int PhotoCount,
    bool HasMusic);

/// <summary>Ficha completa de la ciudad.</summary>
public sealed record CityDetailResponse(
    Guid Id,
    string Name,
    string Country,
    string CountryCode,
    double Latitude,
    double Longitude,
    DateOnly? VisitedOn,
    string? Notes,
    long? Population,
    string? PopulationSource,
    string? ShortDescription,
    string? TypicalFood,
    string? WikidataId,
    string? WikipediaUrl,
    DateTimeOffset? EnrichedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PhotoResponse> Photos,
    IReadOnlyList<MusicSourceResponse> MusicSources);

/// <summary>
/// Mapeo de entidad a DTO. Las entidades de dominio nunca se serializan
/// directamente hacia el cliente.
/// </summary>
public static class CityMappings
{
    public static CitySummaryResponse ToSummary(this City city) => new(
        city.Id,
        city.Name,
        city.Country,
        city.CountryCode.Value,
        city.Coordinates.Latitude,
        city.Coordinates.Longitude,
        city.VisitedOn,
        city.CoverPhoto is null ? null : PhotoMappings.ThumbnailUrlFor(city.CoverPhoto),
        city.Photos.Count,
        city.MusicSources.Count > 0);

    public static CityDetailResponse ToDetail(this City city) => new(
        city.Id,
        city.Name,
        city.Country,
        city.CountryCode.Value,
        city.Coordinates.Latitude,
        city.Coordinates.Longitude,
        city.VisitedOn,
        city.Notes,
        city.Population,
        city.PopulationSource,
        city.ShortDescription,
        city.TypicalFood,
        city.WikidataId,
        city.WikipediaUrl,
        city.EnrichedAt,
        city.CreatedAt,
        city.UpdatedAt,
        city.Photos.ToGallery(),
        city.MusicSources.ToPlaylist());
}
