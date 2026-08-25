using AlbumViajes.Api.Auth;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Auth;
using AlbumViajes.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapGet("/me", (ICurrentUser currentUser) =>
                Results.Ok(new CurrentUserResponse(currentUser.IsOwner, currentUser.Email)))
            .WithName("GetCurrentUser")
            .WithSummary("Dice si quien mira el album puede editarlo.");

        auth.MapGet("/login", (IOptions<AuthOptions> options) =>
            {
                if (!options.Value.IsGoogleConfigured)
                {
                    return Results.Problem(
                        title: "auth.notConfigured",
                        detail: "El album no tiene configuradas las credenciales de Google.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                // "consent" se pide en cada entrada porque Google solo emite el
                // refresh token cuando el usuario concede el permiso de forma
                // explicita, y sin el no se pueden importar fotos.
                var properties = new GoogleChallengeProperties
                {
                    RedirectUri = options.Value.PostLoginRedirect,
                    Prompt = "consent",
                };

                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            })
            .WithName("Login")
            .WithSummary("Empieza la entrada con Google.");

        // Sin redireccion: lo llama el propio album con fetch y se limita a
        // borrar la cookie. Redirigir devolveria el HTML del sitio como respuesta.
        auth.MapPost("/logout", () =>
                Results.SignOut(properties: null, [CookieAuthenticationDefaults.AuthenticationScheme]))
            .WithName("Logout")
            .WithSummary("Cierra la sesion del administrador.");

        return app;
    }
}
