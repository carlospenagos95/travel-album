using System.Security.Cryptography;
using AlbumViajes.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace AlbumViajes.Infrastructure.Auth;

/// <summary>
/// Cifra con las claves de Data Protection, que viven en un volumen del servidor.
/// Si esas claves se pierden, los tokens guardados dejan de poder descifrarse:
/// por eso <see cref="Unprotect"/> devuelve null en vez de reventar, y el usuario
/// simplemente vuelve a conceder el permiso.
/// </summary>
internal sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "AlbumViajes.GoogleRefreshToken.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector(Purpose);

    public string Protect(string secret) => _protector.Protect(secret);

    public string? Unprotect(string protectedSecret)
    {
        try
        {
            return _protector.Unprotect(protectedSecret);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
