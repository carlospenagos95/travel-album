using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.ValueObjects;

/// <summary>
/// Video de YouTube identificado por su id. Acepta lo que el usuario tenga a mano
/// (el enlace de la barra de direcciones, el de compartir, el de incrustar o el
/// id suelto) y guarda siempre el id, que es lo unico que necesita el reproductor.
/// </summary>
public readonly record struct YouTubeVideo
{
    private const int IdLength = 11;

    private YouTubeVideo(string value) => Value = value;

    public string Value { get; }

    public static Result<YouTubeVideo> Create(string urlOrId)
    {
        var candidate = ExtractCandidate(urlOrId?.Trim());

        if (candidate is null || !IsWellFormed(candidate))
        {
            return Error.Validation(
                "music.youTube",
                "Pega el enlace de un video de YouTube o su identificador de 11 caracteres.");
        }

        return new YouTubeVideo(candidate);
    }

    public override string ToString() => Value;

    private static string? ExtractCandidate(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            // Sin esquema no es una URL: se toma tal cual como id.
            return input;
        }

        // youtu.be/ID y youtube.com/{embed,shorts,live}/ID llevan el id en la ruta.
        var lastSegment = uri.Segments.Length == 0 ? null : uri.Segments[^1].TrimEnd('/');

        if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return lastSegment;
        }

        var queryVideoId = ReadQueryParameter(uri.Query, "v");

        return queryVideoId ?? lastSegment;
    }

    private static string? ReadQueryParameter(string query, string name)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);

            if (separator > 0 && pair[..separator].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[(separator + 1)..]);
            }
        }

        return null;
    }

    private static bool IsWellFormed(string candidate) =>
        candidate.Length == IdLength && candidate.All(IsIdCharacter);

    private static bool IsIdCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_';
}
