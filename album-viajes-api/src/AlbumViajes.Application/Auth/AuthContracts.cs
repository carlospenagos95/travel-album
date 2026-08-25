namespace AlbumViajes.Application.Auth;

/// <summary>
/// Quien esta viendo el album. El frontend lo usa para decidir si muestra los
/// controles de edicion; la autorizacion real la sigue haciendo el servidor.
/// </summary>
public sealed record CurrentUserResponse(bool IsOwner, string? Email);
