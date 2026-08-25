using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace AlbumViajes.Tests.Integration;

/// <summary>
/// Sustituye a Google Photos. Devuelve imagenes reales, no bytes cualquiera,
/// porque el almacenamiento genera miniaturas y tiene que poder decodificarlas:
/// probar con contenido falso no probaria nada.
/// </summary>
public sealed class StubPhotoLibrary : IPhotoLibrary
{
    public const string SessionId = "sesion-de-prueba";

    /// <summary>Fotos que el usuario habra elegido cuando el test pida importar.</summary>
    public List<PickedPhoto> Picked { get; } = [];

    /// <summary>Con false, la sesion simula que el usuario todavia esta eligiendo.</summary>
    public bool PhotosPicked { get; set; } = true;

    public bool SessionDiscarded { get; private set; }

    public void Reset()
    {
        Picked.Clear();
        Picked.Add(PhotoNamed("playa.jpg"));
        PhotosPicked = true;
        SessionDiscarded = false;
    }

    public static PickedPhoto PhotoNamed(string fileName) => new(
        MediaId: $"media-{fileName}",
        FileName: fileName,
        MimeType: "image/jpeg",
        Width: 800,
        Height: 600,
        TakenAt: new DateTimeOffset(2024, 7, 14, 10, 30, 0, TimeSpan.Zero),
        SourceReference: $"https://ejemplo.invalido/{fileName}");

    public Task<Result<PhotoPickerSession>> StartPickerSessionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result<PhotoPickerSession>.Success(CurrentSession()));

    public Task<Result<PhotoPickerSession>> GetPickerSessionAsync(string sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(Result<PhotoPickerSession>.Success(CurrentSession()));

    public Task<Result<IReadOnlyList<PickedPhoto>>> ListPickedPhotosAsync(
        string sessionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<IReadOnlyList<PickedPhoto>>.Success(Picked.ToArray()));

    public async Task<Result<PhotoContent>> DownloadAsync(PickedPhoto photo, CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(photo.Width, photo.Height);

        var content = new MemoryStream();
        await image.SaveAsJpegAsync(content, new JpegEncoder { Quality = 70 }, cancellationToken);
        content.Position = 0;

        return new PhotoContent(content, photo.MimeType);
    }

    public Task DiscardPickerSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        SessionDiscarded = true;
        return Task.CompletedTask;
    }

    private PhotoPickerSession CurrentSession() =>
        new(SessionId, $"https://photos.google.com/picker/{SessionId}", PhotosPicked, 5);
}
