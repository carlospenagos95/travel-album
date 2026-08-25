namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Cifra los secretos que hay que guardar en la base de datos. La aplicacion no
/// sabe con que algoritmo ni con que clave: solo que nunca escribe texto plano.
/// </summary>
public interface ISecretProtector
{
    string Protect(string secret);

    /// <summary>Devuelve null si el texto no se puede descifrar, por ejemplo tras perder las claves.</summary>
    string? Unprotect(string protectedSecret);
}
