using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Cancion tal como la publica el catalogo, antes de que el usuario la elija.
/// <paramref name="AudioUrl"/> es una muestra de treinta segundos y
/// <paramref name="DurationSeconds"/>, la duracion de la cancion entera.
/// </summary>
public sealed record LibraryTrack(
    string Id,
    string Name,
    string? ArtistName,
    string AudioUrl,
    int DurationSeconds,
    string? ImageUrl);

/// <summary>Puerto hacia el catalogo de canciones.</summary>
public interface IMusicLibrary
{
    Task<Result<IReadOnlyList<LibraryTrack>>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}
