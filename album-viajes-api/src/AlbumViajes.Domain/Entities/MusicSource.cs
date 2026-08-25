using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Domain.Entities;

/// <summary>De donde sale la musica que suena al abrir la ciudad.</summary>
public enum MusicKind
{
    RadioStation,
    YouTube,
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
        string? youTubeVideoId,
        DateTimeOffset createdAt)
        : base(id)
    {
        CityId = cityId;
        Kind = kind;
        Label = label;
        RadioStationUuid = radioStationUuid;
        RadioStreamUrl = radioStreamUrl;
        YouTubeVideoId = youTubeVideoId;
        CreatedAt = createdAt;
    }

    public Guid CityId { get; private set; }

    public MusicKind Kind { get; private set; }

    /// <summary>Nombre visible: la emisora o el titulo del video.</summary>
    public string Label { get; private set; }

    /// <summary>Suena al abrir la ciudad. Solo una fuente por ciudad puede serlo.</summary>
    public bool IsDefault { get; private set; }

    public string? RadioStationUuid { get; private set; }

    /// <summary>
    /// Se guarda como texto y no como <see cref="Uri"/> para que la fila sea
    /// legible y portable; el value object valida antes de llegar aqui.
    /// </summary>
    public string? RadioStreamUrl { get; private set; }

    public string? YouTubeVideoId { get; private set; }

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
            youTubeVideoId: null,
            now);
    }

    internal static Result<MusicSource> ForYouTube(Guid cityId, string urlOrId, string? title, DateTimeOffset now)
    {
        var video = YouTubeVideo.Create(urlOrId);
        if (!video.IsSuccess)
        {
            return video.Error!;
        }

        // Sin titulo el enlace sigue siendo utilizable: se etiqueta con el id.
        var label = NormalizeLabel(
            string.IsNullOrWhiteSpace(title) ? video.Value.Value : title,
            "music.title",
            "titulo del video");

        if (!label.IsSuccess)
        {
            return label.Error!;
        }

        return new MusicSource(
            Guid.NewGuid(),
            cityId,
            MusicKind.YouTube,
            label.Value,
            radioStationUuid: null,
            radioStreamUrl: null,
            video.Value.Value,
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

        return trimmed.Length > MaxLabelLength ? trimmed[..MaxLabelLength] : trimmed;
    }
}
