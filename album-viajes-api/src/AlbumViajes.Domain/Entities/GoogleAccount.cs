using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.Entities;

/// <summary>
/// Cuenta de Google del duenio del album, con el permiso concedido para leer las
/// fotos que elija en el selector.
///
/// Se persiste el refresh token y no el access token porque el segundo caduca en
/// una hora: sin el primero, importar fotos exigiria volver a pasar por Google
/// cada vez. El token llega ya cifrado desde la infraestructura; el dominio no
/// sabe como se protege, solo que no debe guardarse en claro.
/// </summary>
public sealed class GoogleAccount : Entity
{
    public const int MaxEmailLength = 320;

    /// <summary>Solo lo usa EF Core al materializar filas. El dominio nunca lo llama.</summary>
    private GoogleAccount()
        : base(Guid.Empty)
    {
        Email = null!;
        ProtectedRefreshToken = null!;
    }

    private GoogleAccount(Guid id, string email, string protectedRefreshToken, DateTimeOffset now)
        : base(id)
    {
        Email = email;
        ProtectedRefreshToken = protectedRefreshToken;
        ConnectedAt = now;
        UpdatedAt = now;
    }

    /// <summary>En minusculas: es la clave con la que se compara contra la lista de permitidos.</summary>
    public string Email { get; private set; }

    public string ProtectedRefreshToken { get; private set; }

    public DateTimeOffset ConnectedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<GoogleAccount> Create(string email, string protectedRefreshToken, DateTimeOffset now)
    {
        var normalized = NormalizeEmail(email);
        if (!normalized.IsSuccess)
        {
            return normalized.Error!;
        }

        if (string.IsNullOrWhiteSpace(protectedRefreshToken))
        {
            return MissingToken;
        }

        return new GoogleAccount(Guid.NewGuid(), normalized.Value, protectedRefreshToken, now);
    }

    public Result RenewConsent(string protectedRefreshToken, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(protectedRefreshToken))
        {
            return MissingToken;
        }

        ProtectedRefreshToken = protectedRefreshToken;
        UpdatedAt = now;
        return Result.Success();
    }

    private static Error MissingToken =>
        Error.Validation("google.refreshToken", "Google no devolvio un refresh token: vuelve a conceder el permiso.");

    private static Result<string> NormalizeEmail(string email)
    {
        var trimmed = email?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxEmailLength || !trimmed.Contains('@', StringComparison.Ordinal))
        {
            return Error.Validation("google.email", "El correo de la cuenta de Google no es valido.");
        }

        return trimmed;
    }
}
