using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>
/// Busca canciones en el catalogo para que el usuario elija una. Igual que con
/// las emisoras, el backend hace de intermediario: cachea las respuestas en un
/// solo sitio y mantiene la busqueda dentro del ritmo que admite el catalogo.
/// </summary>
public sealed class SearchTracksHandler(IMusicLibrary library)
{
    private const int MinQueryLength = 2;
    private const int ResultLimit = 20;

    public async Task<Result<IReadOnlyList<LibraryTrackResponse>>> HandleAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;

        if (trimmed.Length < MinQueryLength)
        {
            return Error.Validation("music.query", $"Escribe al menos {MinQueryLength} caracteres para buscar canciones.");
        }

        var tracks = await library.SearchAsync(trimmed, ResultLimit, cancellationToken);
        if (!tracks.IsSuccess)
        {
            return tracks.Error!;
        }

        IReadOnlyList<LibraryTrackResponse> response = tracks.Value
            .Select(track => new LibraryTrackResponse(
                track.Id,
                track.Name,
                track.ArtistName,
                track.AudioUrl,
                track.DurationSeconds,
                track.ImageUrl))
            .ToArray();

        return Result<IReadOnlyList<LibraryTrackResponse>>.Success(response);
    }
}
