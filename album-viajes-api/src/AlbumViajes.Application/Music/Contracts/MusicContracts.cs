using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Music.Contracts;

/// <summary>
/// Fuente de musica lista para reproducir. Siempre se expone la URL del proxy
/// propio, nunca la del origen: muchas emisoras siguen en http plano y el
/// navegador las bloquearia al servir el album por https, y las muestras de
/// iTunes llegan con un tipo de contenido que no todos los navegadores
/// reconocen. De paso, quien mira el album no le deja su direccion a Apple.
/// </summary>
public sealed record MusicSourceResponse(
    Guid Id,
    string Kind,
    string Label,
    bool IsDefault,
    string StreamUrl,
    string? Artist);

public sealed record RadioStationResponse(
    string Uuid,
    string Name,
    string StreamUrl,
    string? Country,
    string? CountryCode,
    string? Tags,
    int Votes,
    string? Codec,
    int Bitrate);

public sealed record LibraryTrackResponse(
    string Id,
    string Name,
    string? ArtistName,
    string AudioUrl,
    int DurationSeconds,
    string? ImageUrl);

public sealed record AddRadioStationRequest(string StationUuid, string StationName, string StreamUrl);

public sealed record AddTrackRequest(string TrackId, string Title, string? Artist, string AudioUrl);

public static class MusicMappings
{
    public static MusicSourceResponse ToResponse(this MusicSource source) => new(
        source.Id,
        source.Kind.ToString(),
        source.Label,
        source.IsDefault,
        StreamUrlFor(source),
        source.TrackArtist);

    public static string StreamUrlFor(MusicSource source) =>
        $"/api/cities/{source.CityId}/music/{source.Id}/stream";

    /// <summary>La que suena primero encabeza la lista.</summary>
    public static IReadOnlyList<MusicSourceResponse> ToPlaylist(this IEnumerable<MusicSource> sources) =>
        sources
            .OrderByDescending(source => source.IsDefault)
            .ThenBy(source => source.CreatedAt)
            .Select(ToResponse)
            .ToArray();
}
