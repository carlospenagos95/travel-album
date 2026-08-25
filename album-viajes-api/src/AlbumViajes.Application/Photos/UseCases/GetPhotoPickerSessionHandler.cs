using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Photos.Contracts;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Photos.UseCases;

/// <summary>
/// Consulta si el usuario ya termino de elegir. El frontend pregunta cada
/// <see cref="PhotoPickerSessionResponse.PollIntervalSeconds"/> segundos, que es
/// el ritmo que pide el propio Google.
/// </summary>
public sealed class GetPhotoPickerSessionHandler(IPhotoLibrary library)
{
    public async Task<Result<PhotoPickerSessionResponse>> HandleAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await library.GetPickerSessionAsync(sessionId, cancellationToken);

        return session.IsSuccess
            ? StartPhotoPickerSessionHandler.ToResponse(session.Value)
            : session.Error!;
    }
}
