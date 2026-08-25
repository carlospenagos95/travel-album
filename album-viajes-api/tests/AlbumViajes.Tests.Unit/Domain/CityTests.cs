using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly CountryCode Colombia = CountryCode.Create("CO").Value;
    private static readonly Coordinates Medellin = Coordinates.Create(6.2442, -75.5812).Value;

    [Fact]
    public void Create_recorta_los_textos_y_marca_las_fechas()
    {
        var result = City.Create("  Medellin  ", "  Colombia ", Colombia, Medellin, Now);

        Assert.True(result.IsSuccess);
        var city = result.Value;
        Assert.Equal("Medellin", city.Name);
        Assert.Equal("Colombia", city.Country);
        Assert.Equal(Now, city.CreatedAt);
        Assert.Equal(Now, city.UpdatedAt);
        Assert.Null(city.EnrichedAt);
        Assert.NotEqual(Guid.Empty, city.Id);
    }

    [Theory]
    [InlineData("", "city.name")]
    [InlineData("   ", "city.name")]
    public void Create_exige_nombre(string name, string expectedCode)
    {
        var result = City.Create(name, "Colombia", Colombia, Medellin, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void Create_rechaza_nombres_demasiado_largos()
    {
        var result = City.Create(new string('a', City.MaxNameLength + 1), "Colombia", Colombia, Medellin, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("city.name", result.Error!.Code);
    }

    [Fact]
    public void RecordVisit_rechaza_fechas_futuras()
    {
        var city = NewCity();

        var result = city.RecordVisit(DateOnly.FromDateTime(Now.UtcDateTime).AddDays(1), null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("city.visitedOn", result.Error!.Code);
        Assert.Null(city.VisitedOn);
    }

    [Fact]
    public void RecordVisit_acepta_hoy_y_convierte_notas_vacias_en_null()
    {
        var city = NewCity();
        var today = DateOnly.FromDateTime(Now.UtcDateTime);

        var result = city.RecordVisit(today, "   ", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(today, city.VisitedOn);
        Assert.Null(city.Notes);
    }

    [Fact]
    public void Relocate_cambia_las_coordenadas_y_actualiza_UpdatedAt()
    {
        var city = NewCity();
        var later = Now.AddHours(3);
        var bogota = Coordinates.Create(4.7110, -74.0721).Value;

        city.Relocate(bogota, later);

        Assert.Equal(bogota, city.Coordinates);
        Assert.Equal(later, city.UpdatedAt);
        Assert.Equal(Now, city.CreatedAt);
    }

    [Fact]
    public void ApplyEnrichment_no_borra_la_descripcion_existente_si_la_fuente_no_trae_una()
    {
        var city = NewCity();
        city.DescribeManually("Descripcion escrita a mano", "Bandeja paisa", Now);
        var later = Now.AddDays(1);

        city.ApplyEnrichment(2_500_000, "Wikidata", shortDescription: null, "Q48278", "https://es.wikipedia.org/wiki/Medellin", later);

        Assert.Equal("Descripcion escrita a mano", city.ShortDescription);
        Assert.Equal(2_500_000, city.Population);
        Assert.Equal("Q48278", city.WikidataId);
        Assert.Equal(later, city.EnrichedAt);
    }

    [Fact]
    public void ApplyEnrichment_nunca_toca_la_comida_tipica()
    {
        var city = NewCity();
        city.DescribeManually(null, "Bandeja paisa", Now);

        city.ApplyEnrichment(1, "Wikidata", "Otra descripcion", "Q1", null, Now.AddDays(1));

        Assert.Equal("Bandeja paisa", city.TypicalFood);
    }

    [Fact]
    public void DescribeManually_rechaza_descripciones_demasiado_largas()
    {
        var city = NewCity();

        var result = city.DescribeManually(new string('a', City.MaxShortDescriptionLength + 1), null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("city.shortDescription", result.Error!.Code);
    }

    private static City NewCity() => City.Create("Medellin", "Colombia", Colombia, Medellin, Now).Value;
}
