using System.Security.Claims;
using AlbumViajes.Application.Auth.UseCases;
using AlbumViajes.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Api.Auth;

/// <summary>
/// Entrada del duenio del album con Google.
///
/// El album se lee sin identificarse; esto solo existe para editar. La sesion se
/// guarda en una cookie propia y no se usa el token de Google como credencial:
/// ese token solo sirve para leer las fotos que el usuario elija.
/// </summary>
internal static class OwnerAuthentication
{
    /// <summary>Marca en la cookie que la sesion pertenece a un administrador.</summary>
    public const string RoleClaimType = "album_role";

    public const string OwnerRole = "owner";

    /// <summary>Nombre de la politica que protege todo lo que modifica el album.</summary>
    public const string OwnerPolicy = "Owner";

    public const string CallbackPath = "/api/auth/google/callback";

    public static IServiceCollection AddOwnerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        var builder = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(ConfigureCookie);

        // Sin credenciales configuradas no se registra el esquema: la aplicacion
        // arranca igual y solo queda deshabilitada la entrada.
        if (options.IsGoogleConfigured)
        {
            builder.AddGoogle(google => ConfigureGoogle(google, options));
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(OwnerPolicy, policy => policy.RequireClaim(RoleClaimType, OwnerRole));

        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions cookie)
    {
        cookie.Cookie.Name = "albumviajes.session";
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SameSite = SameSiteMode.Lax;

        // En desarrollo el sitio va por http; en produccion siempre por https.
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        cookie.ExpireTimeSpan = TimeSpan.FromDays(30);
        cookie.SlidingExpiration = true;

        // Esto es una API: ante una peticion sin permiso se responde con un codigo,
        // no con una redireccion a una pagina de login que no existe.
        cookie.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        cookie.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }

    private static void ConfigureGoogle(GoogleOptions google, AuthOptions options)
    {
        google.ClientId = options.Google.ClientId;
        google.ClientSecret = options.Google.ClientSecret;
        google.CallbackPath = CallbackPath;

        google.Scope.Add(Infrastructure.Photos.GooglePhotosOptions.Scope);

        // "offline" es lo que hace que Google entregue un refresh token; sin el,
        // importar fotos exigiria volver a autorizar cada hora. El "prompt=consent"
        // que lo acompania lo pone el endpoint de entrada, al lanzar el desafio.
        google.AccessType = "offline";

        // El token no viaja en la cookie: se guarda cifrado en la base de datos.
        google.SaveTokens = false;

        google.Events.OnCreatingTicket = OnCreatingTicketAsync;
        google.Events.OnRemoteFailure = OnRemoteFailure;
    }

    /// <summary>
    /// Ultimo paso del ciclo con Google: se comprueba la lista de permitidos y se
    /// guarda el consentimiento. Si algo falla se lanza a proposito, porque el
    /// manejador lo traduce en un fallo remoto y de ahi sale la redireccion con
    /// el motivo.
    /// </summary>
    private static async Task OnCreatingTicketAsync(OAuthCreatingTicketContext context)
    {
        var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        var handler = context.HttpContext.RequestServices.GetRequiredService<SignInOwnerHandler>();
        var result = await handler.HandleAsync(email, context.RefreshToken, context.HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            throw new OwnerSignInException(result.Error!.Message);
        }

        context.Identity!.AddClaim(new Claim(RoleClaimType, OwnerRole));
    }

    private static Task OnRemoteFailure(RemoteFailureContext context)
    {
        var reason = context.Failure is OwnerSignInException signIn
            ? signIn.Message
            : "No se pudo completar la entrada con Google.";

        var target = QueryHelpers.AddQueryString(RedirectTargetOf(context), "authError", reason);

        context.Response.Redirect(target);
        context.HandleResponse();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Vuelve al album para que el mensaje se vea dentro de la aplicacion.
    ///
    /// No basta con las propiedades de la peticion: cuando el fallo ocurre antes
    /// de recuperarlas, llegan vacias, y una ruta relativa se resolveria contra
    /// el host de la API, que no sirve el album. Por eso el destino configurado
    /// es la ultima palabra y no el ultimo recurso.
    /// </summary>
    private static string RedirectTargetOf(RemoteFailureContext context)
    {
        if (context.Properties?.RedirectUri is { Length: > 0 } stored)
        {
            return stored;
        }

        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<AuthOptions>>();

        return options.Value.PostLoginRedirect;
    }
}

/// <summary>
/// Rechazo de la entrada por una razon que el usuario puede entender y corregir,
/// a diferencia de un fallo tecnico del proveedor.
/// </summary>
internal sealed class OwnerSignInException(string message) : Exception(message);
