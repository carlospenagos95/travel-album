using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Domain.Entities;

/// <summary>De donde sale la musica que suena al abrir la ciudad.</summary>
public enum MusicKind
{
    RadioStation,
    Track,
}

/// <summary>
/// Una fuente de musica asociada a una ciudad. Las dos variantes comparten tabla
/// porque comparten ciclo de vida y solo difieren en un par de campos; cual de
/// ellos aplica lo decide <see cref="Kind"/>.
/// Vive dentro del agregado <see cref="City"/>.
/// </summary>
public sealed class MusicSource : Entity
{
    public const int MaxLabelLength = 200;
    public const int MaxTrackIdLength = 32;

    /// <summary>Solo lo usa EF Core al materializar filas. El dominio nunca lo llama.</summary>
    private MusicSource()
        : base(Guid.Empty) => Label = null!;

    private MusicSource(
        Guid id,
        Guid cityId,
        MusicKind kind,
        string label,
        string? radioStationUuid,
        string? radioStreamUrl,
        string? trackId,
        string? trackArtist,
        string? trackAudioUrl,
        DateTimeOffset createdAt)
        : base(id)
    {
        CityId = cityId;
        Kind = kind;
        Label = label;
        RadioStationUuid = radioStationUuid;
        RadioStreamUrl = radioStreamUrl;
        TrackId = trackId;
        TrackArtist = trackArtist;
        TrackAudioUrl = trackAudioUrl;
        CreatedAt = createdAt;
    }

    public Guid CityId { get; private set; }

    public MusicKind Kind { get; private set; }

    /// <summary>Nombre visible: la emisora o el titulo de la cancion.</summary>
    public string Label { get; private set; }

    /// <summary>Suena al abrir la ciudad. Solo una fuente por ciudad puede serlo.</summary>
    public bool IsDefault { get; private set; }

    public string? RadioStationUuid { get; private set; }

    /// <summary>
    /// Se guarda como texto y no como <see cref="Uri"/> para que la fila sea
    /// legible y portable; el value object valida antes de llegar aqui.
    /// </summary>
    public string? RadioStreamUrl { get; private set; }

    /// <summary>Identificador de la cancion en el catalogo externo.</summary>
    public string? TrackId { get; private set; }

    public string? TrackArtist { get; private set; }

    /// <summary>
    /// Audio que publica el catalogo. Como el de la radio, se sirve a traves del
    /// proxy y no directamente al navegador.
    /// </summary>
    public string? TrackAudioUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static Result<MusicSource> ForRadio(
        Guid cityId,
        string stationUuid,
        string stationName,
        string streamUrl,
        DateTimeOffset now)
    {
        var label = NormalizeLabel(stationName, "music.stationName", "nombre de la emisora");
        if (!label.IsSuccess)
        {
            return label.Error!;
        }

        var uuid = stationUuid?.Trim();
        if (string.IsNullOrEmpty(uuid))
        {
            return Error.Validation("music.stationUuid", "Falta el identificador de la emisora.");
        }

        var url = StreamUrl.Create(streamUrl);
        if (!url.IsSuccess)
        {
            return url.Error!;
        }

        return new MusicSource(
            Guid.NewGuid(),
            cityId,
            MusicKind.RadioStation,
            label.Value,
            uuid,
            url.Value.ToString(),
            trackId: null,
            trackArtist: null,
            trackAudioUrl: null,
            now);
    }

    internal static Result<MusicSource> ForTrack(
        Guid cityId,
        string trackId,
        string title,
        string? artist,
        string audioUrl,
        DateTimeOffset now)
    {
        var label = NormalizeLabel(title, "music.trackTitle", "titulo de la cancion");
        if (!label.IsSuccess)
        {
            return label.Error!;
        }

        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id) || id.Length > MaxTrackIdLength)
        {
            return Error.Validation("music.trackId", "Falta el identificador de la cancion.");
        }

        var url = StreamUrl.Create(audioUrl);
        if (!url.IsSuccess)
        {
            return url.Error!;
        }

        var normalizedArtist = artist?.Trim();

        return new MusicSource(
            Guid.NewGuid(),
            cityId,
            MusicKind.Track,
            label.Value,
            radioStationUuid: null,
            radioStreamUrl: null,
            id,
            // El artista es informativo: sin el la cancion sigue sonando.
            string.IsNullOrEmpty(normalizedArtist) ? null : Truncate(normalizedArtist),
            url.Value.ToString(),
            now);
    }

    internal void MarkAsDefault(bool isDefault) => IsDefault = isDefault;

    private static Result<string> NormalizeLabel(string? value, string code, string label)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Error.Validation(code, $"El {label} es obligatorio.");
        }

        return Truncate(trimmed);
    }

    private static string Truncate(string value) =>
        value.Length > MaxLabelLength ? value[..MaxLabelLength] : value;
}
