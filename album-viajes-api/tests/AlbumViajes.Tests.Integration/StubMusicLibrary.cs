using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Tests.Integration;

/// <summary>Sustituye al buscador de iTunes: la suite no depende de su catalogo ni de la red.</summary>
public sealed class StubMusicLibrary : IMusicLibrary
{
    public static readonly LibraryTrack Track = new(
        Id: "1440913214",
        Name: "La Gota Fria",
        ArtistName: "Carlos Vives",
        AudioUrl: "https://audio-ssl.itunes.apple.invalido/preview/la-gota-fria.m4a",
        DurationSeconds: 267,
        ImageUrl: "https://is1-ssl.mzstatic.invalido/image/thumb/100x100bb.jpg");

    public Result<IReadOnlyList<LibraryTrack>> NextResult { get; set; } = Default;

    private static Result<IReadOnlyList<LibraryTrack>> Default =>
        Result<IReadOnlyList<LibraryTrack>>.Success([Track]);

    public void Reset() => NextResult = Default;

    public void FailWith(Error error) => NextResult = Result<IReadOnlyList<LibraryTrack>>.Failure(error);

    public Task<Result<IReadOnlyList<LibraryTrack>>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken) => Task.FromResult(NextResult);
}
