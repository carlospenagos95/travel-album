using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.ValueObjects;

/// <summary>
/// Direccion de un stream de audio. Muchas emisoras del directorio publican
/// todavia URLs en http plano; se aceptan porque el backend las reenvia sobre
/// https, pero cualquier otro esquema queda fuera.
/// </summary>
public readonly record struct StreamUrl
{
    public const int MaxLength = 500;

    private StreamUrl(Uri value) => Value = value;

    public Uri Value { get; }

    public static Result<StreamUrl> Create(string url)
    {
        var trimmed = url?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            return Error.Validation("music.streamUrl", $"La direccion del stream es obligatoria y no puede superar {MaxLength} caracteres.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Error.Validation("music.streamUrl", "La direccion del stream debe ser una URL http o https.");
        }

        return new StreamUrl(uri);
    }

    public override string ToString() => Value.ToString();
}
