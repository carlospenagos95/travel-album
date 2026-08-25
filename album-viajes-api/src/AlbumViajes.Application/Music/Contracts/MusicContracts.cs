using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Music.Contracts;

/// <summary>
/// Fuente de musica lista para reproducir. Para la radio se expone la URL del
/// proxy propio, nunca la del stream original: muchas siguen en http plano y el
/// navegador las bloquearia al servir el album por https.
/// </summary>
public sealed record MusicSourceResponse(
    Guid Id,
    string Kind,
    string Label,
    bool IsDefault,
    string? StreamUrl,
    string? YouTubeVideoId);

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

public sealed record AddRadioStationRequest(string StationUuid, string StationName, string StreamUrl);

public sealed record AddYouTubeVideoRequest(string UrlOrId, string? Title);

public static class MusicMappings
{
    public static MusicSourceResponse ToResponse(this MusicSource source) => new(
        source.Id,
        source.Kind.ToString(),
        source.Label,
        source.IsDefault,
        source.Kind == MusicKind.RadioStation ? StreamUrlFor(source) : null,
        source.YouTubeVideoId);

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
