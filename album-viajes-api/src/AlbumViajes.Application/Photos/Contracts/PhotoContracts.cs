using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Photos.Contracts;

/// <summary>Foto tal como la consume la galeria.</summary>
public sealed record PhotoResponse(
    Guid Id,
    string FileName,
    string? Caption,
    int Width,
    int Height,
    DateTimeOffset? TakenAt,
    int SortOrder,
    string FileUrl,
    string ThumbnailUrl);

/// <summary>Sesion del selector de Google, tal como la sigue el frontend.</summary>
public sealed record PhotoPickerSessionResponse(
    string SessionId,
    string PickerUri,
    bool PhotosPicked,
    int PollIntervalSeconds);

/// <summary>
/// Resultado de una importacion. Se informa cuantas se saltaron porque el usuario
/// puede volver a elegir fotos que ya estaban en el album.
/// </summary>
public sealed record PhotoImportResponse(int Imported, int Skipped, IReadOnlyList<PhotoResponse> Photos);

public sealed record ReorderPhotosRequest(IReadOnlyList<Guid> PhotoIds);

public sealed record CaptionPhotoRequest(string? Caption);

public static class PhotoMappings
{
    /// <summary>
    /// Las fotos se sirven siempre desde la API y nunca desde Google: el enlace
    /// que devuelve el selector caduca a la hora, el del album no caduca nunca.
    /// </summary>
    public static PhotoResponse ToResponse(this Photo photo) => new(
        photo.Id,
        photo.FileName,
        photo.Caption,
        photo.Width,
        photo.Height,
        photo.TakenAt,
        photo.SortOrder,
        FileUrlFor(photo),
        ThumbnailUrlFor(photo));

    public static string FileUrlFor(Photo photo) => $"/api/cities/{photo.CityId}/photos/{photo.Id}/file";

    public static string ThumbnailUrlFor(Photo photo) => $"/api/cities/{photo.CityId}/photos/{photo.Id}/thumbnail";

    public static IReadOnlyList<PhotoResponse> ToGallery(this IEnumerable<Photo> photos) =>
        photos.OrderBy(photo => photo.SortOrder).Select(ToResponse).ToArray();
}
