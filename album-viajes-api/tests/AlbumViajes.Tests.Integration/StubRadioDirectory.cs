using System.Text;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Tests.Integration;

/// <summary>Sustituye al directorio Radio Browser: la suite no depende de su catalogo.</summary>
public sealed class StubRadioDirectory : IRadioDirectory
{
    public static readonly RadioStation Station = new(
        Uuid: "9c1f0e2a-0000-4000-8000-000000000001",
        Name: "Radio Paisa",
        StreamUrl: "http://stream.ejemplo.invalido/paisa.mp3",
        Country: "Colombia",
        CountryCode: "CO",
        Tags: "salsa,tropical",
        Votes: 1200,
        Codec: "MP3",
        Bitrate: 128);

    public Result<IReadOnlyList<RadioStation>> NextResult { get; set; } = Default;

    private static Result<IReadOnlyList<RadioStation>> Default =>
        Result<IReadOnlyList<RadioStation>>.Success([Station]);

    public void Reset() => NextResult = Default;

    public void FailWith(Error error) => NextResult = Result<IReadOnlyList<RadioStation>>.Failure(error);

    public Task<Result<IReadOnlyList<RadioStation>>> SearchAsync(
        string query,
        string? countryCode,
        int limit,
        CancellationToken cancellationToken) => Task.FromResult(NextResult);
}

/// <summary>
/// Sustituye la lectura del stream de la emisora. Devuelve unos bytes fijos, que
/// es todo lo que el proxy necesita reenviar para poder comprobarse.
/// </summary>
public sealed class StubAudioStreamReader : IAudioStreamReader
{
    public const string Sample = "audio-de-prueba";

    /// <summary>Ultima direccion que se pidio abrir, para comprobar que se proxea la correcta.</summary>
    public Uri? LastRequestedUrl { get; private set; }

    /// <summary>
    /// Lo que responde el origen. Se puede cambiar para comprobar que el tipo de
    /// contenido raro con el que iTunes marca sus muestras no llega al navegador.
    /// </summary>
    public string ContentType { get; set; } = "audio/mpeg";

    public void Reset() => ContentType = "audio/mpeg";

    public Task<Result<AudioFeed>> OpenAsync(Uri streamUrl, CancellationToken cancellationToken)
    {
        LastRequestedUrl = streamUrl;

        return Task.FromResult(Result<AudioFeed>.Success(new AudioFeed(Sampled(), ContentType)));
    }

    public Task<Result<AudioFeed>> DownloadAsync(Uri fileUrl, CancellationToken cancellationToken)
    {
        LastRequestedUrl = fileUrl;

        // Como el de verdad: el fichero llega entero y se puede recorrer.
        return Task.FromResult(Result<AudioFeed>.Success(new AudioFeed(Sampled(), ContentType, SupportsRange: true)));
    }

    private static MemoryStream Sampled() => new(Encoding.UTF8.GetBytes(Sample));
}
