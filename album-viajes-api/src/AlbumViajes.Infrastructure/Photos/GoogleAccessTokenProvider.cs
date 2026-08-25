using System.Globalization;
using System.Text.Json;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Auth;
using AlbumViajes.Domain.Common;
using AlbumViajes.Infrastructure.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlbumViajes.Infrastructure.Photos;

/// <summary>
/// Cambia el refresh token guardado por un access token vigente.
///
/// Se guarda el resultado en memoria hasta poco antes de que caduque porque el
/// frontend consulta la sesion del selector cada pocos segundos: sin cache, cada
/// consulta abriria una peticion al servidor de tokens de Google.
/// </summary>
internal sealed class GoogleAccessTokenProvider(
    HttpClient http,
    IGoogleAccountRepository accounts,
    ISecretProtector protector,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IOptions<AuthOptions> authOptions,
    IOptions<GooglePhotosOptions> photoOptions,
    ILogger<GoogleAccessTokenProvider> logger)
{
    /// <summary>Margen para que el token no caduque mientras se esta usando.</summary>
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(2);

    private readonly AuthOptions _auth = authOptions.Value;
    private readonly GooglePhotosOptions _photos = photoOptions.Value;

    public async Task<Result<string>> GetAsync(CancellationToken cancellationToken)
    {
        var email = currentUser.Email;

        if (!currentUser.IsOwner || string.IsNullOrWhiteSpace(email))
        {
            return AuthErrors.NotConnected();
        }

        if (!_auth.IsGoogleConfigured)
        {
            return PhotoLibraryErrors.NotConfigured();
        }

        var cacheKey = CacheKeyFor(email);

        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        if (account is null)
        {
            return AuthErrors.NotConnected();
        }

        var refreshToken = protector.Unprotect(account.ProtectedRefreshToken);
        if (refreshToken is null)
        {
            // Las claves de Data Protection cambiaron: el token guardado ya no se
            // puede leer y hay que volver a conceder el permiso.
            logger.LogWarning("El refresh token de {Email} no se pudo descifrar.", email);
            return AuthErrors.NotConnected();
        }

        return await ExchangeAsync(cacheKey, refreshToken, cancellationToken);
    }

    private async Task<Result<string>> ExchangeAsync(string cacheKey, string refreshToken, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _auth.Google.ClientId,
            ["client_secret"] = _auth.Google.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        });

        try
        {
            using var response = await http.PostAsync(new Uri(_photos.TokenEndpoint), content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google rechazo el refresh token con codigo {Status}.", (int)response.StatusCode);
                return AuthErrors.NotConnected();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);

            if (!document.RootElement.TryGetProperty("access_token", out var token)
                || token.GetString() is not { Length: > 0 } accessToken)
            {
                return PhotoLibraryErrors.Unavailable();
            }

            cache.Set(cacheKey, accessToken, LifetimeOf(document.RootElement));

            return accessToken;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "Fallo la renovacion del token de Google.");
            return PhotoLibraryErrors.Unavailable();
        }
    }

    private static TimeSpan LifetimeOf(JsonElement tokenResponse)
    {
        var seconds = tokenResponse.TryGetProperty("expires_in", out var expiresIn) && expiresIn.TryGetInt32(out var value)
            ? value
            : 0;

        var lifetime = TimeSpan.FromSeconds(seconds) - ExpiryMargin;

        return lifetime > TimeSpan.Zero ? lifetime : ExpiryMargin;
    }

    private static string CacheKeyFor(string email) =>
        string.Create(CultureInfo.InvariantCulture, $"google-access-token:{email}");
}
