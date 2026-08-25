using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Application.Cities;

/// <summary>Par de value objects que crear y actualizar necesitan construir por igual.</summary>
internal readonly record struct CityLocation(CountryCode CountryCode, Coordinates Coordinates)
{
    public static Result<CityLocation> FromRequest(SaveCityRequest request)
    {
        var countryCode = CountryCode.Create(request.CountryCode);
        if (!countryCode.IsSuccess)
        {
            return countryCode.Error!;
        }

        var coordinates = Coordinates.Create(request.Latitude, request.Longitude);
        if (!coordinates.IsSuccess)
        {
            return coordinates.Error!;
        }

        return new CityLocation(countryCode.Value, coordinates.Value);
    }
}
