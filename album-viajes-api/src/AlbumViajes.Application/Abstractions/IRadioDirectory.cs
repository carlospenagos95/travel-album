using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>Emisora tal como la publica el directorio, antes de que el usuario la elija.</summary>
public sealed record RadioStation(
    string Uuid,
    string Name,
    string StreamUrl,
    string? Country,
    string? CountryCode,
    string? Tags,
    int Votes,
    string? Codec,
    int Bitrate);

/// <summary>Puerto hacia el directorio de emisoras de radio.</summary>
public interface IRadioDirectory
{
    Task<Result<IReadOnlyList<RadioStation>>> SearchAsync(
        string query,
        string? countryCode,
        int limit,
        CancellationToken cancellationToken);
}
