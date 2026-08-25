using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CoordinatesTests
{
    [Theory]
    [InlineData(6.2442, -75.5812)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(0, 0)]
    public void Acepta_coordenadas_dentro_de_rango(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        Assert.True(result.IsSuccess);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
    }

    [Theory]
    [InlineData(90.1, 0, "coordinates.latitude")]
    [InlineData(-90.1, 0, "coordinates.latitude")]
    [InlineData(double.NaN, 0, "coordinates.latitude")]
    [InlineData(0, 180.1, "coordinates.longitude")]
    [InlineData(0, -180.1, "coordinates.longitude")]
    [InlineData(0, double.NaN, "coordinates.longitude")]
    public void Rechaza_coordenadas_fuera_de_rango(double latitude, double longitude, string expectedCode)
    {
        var result = Coordinates.Create(latitude, longitude);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void Leer_el_valor_de_un_resultado_fallido_lanza()
    {
        var result = Coordinates.Create(1000, 0);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
