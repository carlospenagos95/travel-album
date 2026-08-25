using AlbumViajes.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Auth;

/// <summary>
/// La lista de administradores sale de la configuracion, no de la base de datos:
/// asi no hay forma de concederse permisos desde dentro de la aplicacion.
/// </summary>
internal sealed class ConfigurationOwnerAllowlist(IOptions<AuthOptions> options) : IOwnerAllowlist
{
    private readonly HashSet<string> _allowed = options.Value.AllowedEmails
        .Where(email => !string.IsNullOrWhiteSpace(email))
        .Select(email => email.Trim())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Allows(string email) =>
        !string.IsNullOrWhiteSpace(email) && _allowed.Contains(email.Trim());
}
