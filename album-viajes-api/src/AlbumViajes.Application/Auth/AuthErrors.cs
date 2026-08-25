using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Auth;

/// <summary>Errores esperados del area de autenticacion, en un solo lugar.</summary>
public static class AuthErrors
{
    public static Error NotAllowed(string email) =>
        Error.Validation("auth.notAllowed", $"La cuenta {email} no administra este album.");

    public static Error ConsentIncomplete() =>
        Error.Validation(
            "auth.consentIncomplete",
            "Google no entrego el permiso permanente. Revoca el acceso de la aplicacion en tu cuenta y vuelve a entrar.");

    public static Error NotConnected() =>
        Error.External("auth.notConnected", "No hay una cuenta de Google conectada para leer las fotos.");
}
