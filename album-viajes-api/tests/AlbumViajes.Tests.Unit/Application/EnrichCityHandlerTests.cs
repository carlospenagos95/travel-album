using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.UseCases;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;
using NSubstitute;

namespace AlbumViajes.Tests.Unit.Application;

public sealed class EnrichCityHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly ICityRepository _cities = Substitute.For<ICityRepository>();
    private readonly ICityEnricher _enricher = Substitute.For<ICityEnricher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EnrichCityHandler _handler;

    public EnrichCityHandlerTests() =>
        _handler = new EnrichCityHandler(_cities, _enricher, _unitOfWork, new FixedClock(Now));

    [Fact]
    public async Task Guarda_los_datos_traidos_de_las_fuentes_externas()
    {
        var city = ExistingCity();
        _cities.FindAsync(city.Id, Arg.Any<CancellationToken>()).Returns(city);
        Fetch(city).Returns(new CityFacts(
            2_427_129,
            "Wikidata (Q48278), 2020",
            "Capital de Antioquia.",
            "Q48278",
            "https://es.wikipedia.org/wiki/Medellin"));

        var result = await _handler.HandleAsync(city.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2_427_129, result.Value.Population);
        Assert.Equal("Q48278", result.Value.WikidataId);
        Assert.Equal(Now, result.Value.EnrichedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_toca_la_ciudad_si_la_fuente_externa_falla()
    {
        var city = ExistingCity();
        _cities.FindAsync(city.Id, Arg.Any<CancellationToken>()).Returns(city);
        Fetch(city).Returns(Result<CityFacts>.Failure(
            Error.External("city.enrichmentUnavailable", "Sin respuesta.")));

        var result = await _handler.HandleAsync(city.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.External, result.Error!.Kind);
        Assert.Null(city.EnrichedAt);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Devuelve_no_encontrada_si_la_ciudad_no_existe()
    {
        var id = Guid.NewGuid();
        _cities.FindAsync(id, Arg.Any<CancellationToken>()).Returns((City?)null);

        var result = await _handler.HandleAsync(id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("city.notFound", result.Error!.Code);
        await _enricher.DidNotReceive().FetchAsync(
            Arg.Any<string>(),
            Arg.Any<CountryCode>(),
            Arg.Any<Coordinates>(),
            Arg.Any<CancellationToken>());
    }

    private Task<Result<CityFacts>> Fetch(City city) => _enricher.FetchAsync(
        city.Name,
        city.CountryCode,
        city.Coordinates,
        Arg.Any<CancellationToken>());

    private static City ExistingCity() => City.Create(
        "Medellin",
        "Colombia",
        CountryCode.Create("CO").Value,
        Coordinates.Create(6.2442, -75.5812).Value,
        Now.AddDays(-1)).Value;
}
