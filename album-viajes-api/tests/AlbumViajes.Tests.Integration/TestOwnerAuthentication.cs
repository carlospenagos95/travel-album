using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Tests.Integration;

/// <summary>
/// Sustituye la entrada con Google en los tests. Autentica solo si la peticion
/// trae la cabecera acordada, para que la misma suite pueda comprobar las dos
/// caras: que el duenio edita y que un visitante anonimo recibe 401.
/// </summary>
internal sealed class TestOwnerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestOwner";

    public const string HeaderName = "X-Test-Owner";

    public const string OwnerEmail = "duenio@ejemplo.com";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, OwnerEmail),
                new Claim("album_role", "owner"),
            ],
            SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
