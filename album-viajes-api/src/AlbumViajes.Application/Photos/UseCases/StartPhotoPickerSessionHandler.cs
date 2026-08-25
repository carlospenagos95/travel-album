using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>
/// Abre una sesion del selector de Google para una ciudad. La sesion no se
/// persiste: vive lo que dure la eleccion y el frontend la sigue por su id.
/// </summary>
public sealed class StartPhotoPickerSessionHandler(ICityRepository cities, IPhotoLibrary library)
{
    public async Task<Result<PhotoPickerSessionResponse>> HandleAsync(Guid cityId, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(cityId, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(cityId);
        }

        var session = await library.StartPickerSessionAsync(cancellationToken);

        return session.IsSuccess ? ToResponse(session.Value) : session.Error!;
    }

    internal static PhotoPickerSessionResponse ToResponse(PhotoPickerSession session) => new(
        session.Id,
        session.PickerUri,
        session.PhotosPicked,
        session.PollIntervalSeconds);
}
