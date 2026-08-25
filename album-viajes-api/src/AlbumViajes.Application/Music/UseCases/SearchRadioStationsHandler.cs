using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Music.Contracts;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Application.Music.UseCases;

/// <summary>
/// Busca emisoras en el directorio para que el usuario elija una. El backend
/// hace de intermediario: el directorio pide un User-Agent propio y conviene
/// cachear sus respuestas en un solo sitio.
/// </summary>
public sealed class SearchRadioStationsHandler(IRadioDirectory directory)
{
    private const int MinQueryLength = 2;
    private const int ResultLimit = 20;

    public async Task<Result<IReadOnlyList<RadioStationResponse>>> HandleAsync(
        string query,
        string? countryCode,
        CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;

        if (trimmed.Length < MinQueryLength)
        {
            return Error.Validation("music.query", $"Escribe al menos {MinQueryLength} caracteres para buscar emisoras.");
        }

        var stations = await directory.SearchAsync(trimmed, countryCode, ResultLimit, cancellationToken);
        if (!stations.IsSuccess)
        {
            return stations.Error!;
        }

        IReadOnlyList<RadioStationResponse> response = stations.Value
            .Select(station => new RadioStationResponse(
                station.Uuid,
                station.Name,
                station.StreamUrl,
                station.Country,
                station.CountryCode,
                station.Tags,
                station.Votes,
                station.Codec,
                station.Bitrate))
            .ToArray();

        return Result<IReadOnlyList<RadioStationResponse>>.Success(response);
    }
}
