namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Quien puede administrar el album. Es una lista corta de correos y no un
/// sistema de roles: el album tiene un duenio, y el resto del mundo solo lee.
/// </summary>
public interface IOwnerAllowlist
{
    bool Allows(string email);
}
