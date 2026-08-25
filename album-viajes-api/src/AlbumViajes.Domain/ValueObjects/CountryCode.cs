using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.ValueObjects;

/// <summary>Codigo de pais ISO-3166-1 alfa-2, siempre normalizado a mayusculas.</summary>
public readonly record struct CountryCode
{
    private CountryCode(string value) => Value = value;

    public string Value { get; }

    public static Result<CountryCode> Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(normalized) || normalized.Length != 2 || !normalized.All(char.IsAsciiLetterUpper))
        {
            return Error.Validation("countryCode.format", "El codigo de pais debe ser ISO-3166-1 alfa-2, por ejemplo CO.");
        }

        return new CountryCode(normalized);
    }

    public override string ToString() => Value;
}
