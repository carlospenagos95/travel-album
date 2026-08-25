using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CountryCodeTests
{
    [Theory]
    [InlineData("co", "CO")]
    [InlineData("CO", "CO")]
    [InlineData("  pt  ", "PT")]
    public void Normaliza_a_mayusculas_y_recorta_espacios(string input, string expected)
    {
        var result = CountryCode.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C")]
    [InlineData("COL")]
    [InlineData("C1")]
    public void Rechaza_lo_que_no_sea_iso_3166_alfa_2(string? input)
    {
        var result = CountryCode.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal("countryCode.format", result.Error!.Code);
    }
}
