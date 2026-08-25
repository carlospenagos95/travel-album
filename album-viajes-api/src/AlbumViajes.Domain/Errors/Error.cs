namespace AlbumViajes.Domain.Errors;

/// <summary>
/// Fallo esperado del dominio o de la aplicacion. No es una excepcion: viaja
/// dentro de <see cref="Common.Result{T}"/> como valor de retorno.
/// </summary>
public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorKind.Validation);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error External(string code, string message) => new(code, message, ErrorKind.External);
}

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    External,
}
