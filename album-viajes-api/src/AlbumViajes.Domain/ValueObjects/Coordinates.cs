using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.ValueObjects;

/// <summary>
/// Punto geografico validado. Al existir una instancia, latitud y longitud
/// estan garantizadas dentro de rango: nadie mas necesita volver a validarlas.
/// </summary>
public readonly record struct Coordinates
{
    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
        {
            return Error.Validation("coordinates.latitude", "La latitud debe estar entre -90 y 90.");
        }

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            return Error.Validation("coordinates.longitude", "La longitud debe estar entre -180 y 180.");
        }

        return new Coordinates(latitude, longitude);
    }

    public override string ToString() =>
        $"{Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}";
}
