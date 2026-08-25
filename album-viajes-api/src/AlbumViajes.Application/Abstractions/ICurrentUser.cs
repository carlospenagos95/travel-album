namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Quien esta haciendo la peticion. El album se lee sin identificarse, asi que
/// lo normal es que no haya nadie: solo el duenio tiene correo.
/// </summary>
public interface ICurrentUser
{
    bool IsOwner { get; }

    string? Email { get; }
}
