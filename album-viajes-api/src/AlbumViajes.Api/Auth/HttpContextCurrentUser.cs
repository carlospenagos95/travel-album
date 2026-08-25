using System.Security.Claims;
using AlbumViajes.Application.Abstractions;

namespace AlbumViajes.Api.Auth;

/// <summary>
/// Traduce la cookie de sesion al puerto que usa la aplicacion. Es la unica
/// pieza que conoce a la vez HttpContext y el concepto de duenio.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsOwner => User?.HasClaim(OwnerAuthentication.RoleClaimType, OwnerAuthentication.OwnerRole) == true;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    private ClaimsPrincipal? User => accessor.HttpContext?.User;
}
