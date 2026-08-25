namespace AlbumViajes.Infrastructure.Auth;

/// <summary>Credenciales de la aplicacion ante Google, en appsettings bajo "Auth:Google".</summary>
public sealed class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>Quien administra el album y con que credenciales entra, en appsettings bajo "Auth".</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Correos que pueden editar. Es la lista completa de administradores: a quien
    /// no este aqui se le rechaza la entrada, aunque se identifique con Google.
    /// Para ver el album no hace falta entrar.
    /// </summary>
    public IList<string> AllowedEmails { get; } = new List<string>();

    public GoogleAuthOptions Google { get; } = new();

    /// <summary>
    /// A donde vuelve el navegador tras entrar o salir. En produccion el album y
    /// la API comparten origen, asi que basta la raiz; en desarrollo apunta al
    /// servidor de Vite.
    /// </summary>
    public string PostLoginRedirect { get; set; } = "/";

    /// <summary>
    /// Sin credenciales de Google la aplicacion arranca igual: se puede ver y
    /// editar todo menos las fotos, que son lo unico que las necesita.
    /// </summary>
    public bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(Google.ClientId) && !string.IsNullOrWhiteSpace(Google.ClientSecret);
}
