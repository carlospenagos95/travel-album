using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Abstractions;

/// <summary>
/// Sesion del selector de fotos: el usuario abre <see cref="PickerUri"/> en su
/// cuenta de Google, elige, y la sesion pasa a <see cref="PhotosPicked"/>.
/// </summary>
public sealed record PhotoPickerSession(string Id, string PickerUri, bool PhotosPicked, int PollIntervalSeconds);

/// <summary>
/// Foto elegida por el usuario, todavia en el origen.
/// <see cref="SourceReference"/> es opaco para la aplicacion: la implementacion
/// sabe que es (en Google Photos, un enlace que caduca en una hora). Por eso la
/// descarga ocurre inmediatamente despues de listar.
/// </summary>
public sealed record PickedPhoto(
    string MediaId,
    string FileName,
    string MimeType,
    int Width,
    int Height,
    DateTimeOffset? TakenAt,
    string SourceReference);

/// <summary>Contenido descargado. Quien lo recibe se encarga de liberar el stream.</summary>
public sealed record PhotoContent(Stream Content, string MimeType);

/// <summary>
/// Puerto hacia la biblioteca de fotos del usuario. La aplicacion no sabe que
/// detras hay Google: solo que se abre una sesion, se espera a que elija y se
/// descarga lo elegido.
/// </summary>
public interface IPhotoLibrary
{
    Task<Result<PhotoPickerSession>> StartPickerSessionAsync(CancellationToken cancellationToken);

    Task<Result<PhotoPickerSession>> GetPickerSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PickedPhoto>>> ListPickedPhotosAsync(string sessionId, CancellationToken cancellationToken);

    Task<Result<PhotoContent>> DownloadAsync(PickedPhoto photo, CancellationToken cancellationToken);

    /// <summary>Cierra la sesion al terminar. Un fallo aqui no invalida la importacion.</summary>
    Task DiscardPickerSessionAsync(string sessionId, CancellationToken cancellationToken);
}
