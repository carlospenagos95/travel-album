using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Application.Cities.UseCases;
using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;
using AlbumViajes.Tests.Unit.Application;
using NSubstitute;

namespace AlbumViajes.Tests.Unit.Application;

public sealed class CreateCityHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly CountryCode Colombia = CountryCode.Create("CO").Value;

    private readonly ICityRepository _cities = Substitute.For<ICityRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCityHandler _handler;

    public CreateCityHandlerTests() =>
        _handler = new CreateCityHandler(_cities, _unitOfWork, new FixedClock(Now));

    [Fact]
    public async Task Guarda_la_ciudad_y_confirma_la_transaccion()
    {
        _cities.ExistsWithNameAsync("Medellin", Colombia, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(ValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Medellin", result.Value.Name);
        Assert.Equal("CO", result.Value.CountryCode);
        await _cities.Received(1).AddAsync(Arg.Any<City>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rechaza_una_ciudad_repetida_del_mismo_pais()
    {
        _cities.ExistsWithNameAsync("Medellin", Colombia, null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(ValidRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        await _cities.DidNotReceive().AddAsync(Arg.Any<City>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_guarda_nada_si_las_coordenadas_son_invalidas()
    {
        var result = await _handler.HandleAsync(ValidRequest() with { Latitude = 120 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_guarda_nada_si_la_fecha_de_visita_es_futura()
    {
        var future = DateOnly.FromDateTime(Now.UtcDateTime).AddYears(1);

        var result = await _handler.HandleAsync(ValidRequest() with { VisitedOn = future }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("city.visitedOn", result.Error!.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static SaveCityRequest ValidRequest() => new(
        Name: "Medellin",
        Country: "Colombia",
        CountryCode: "co",
        Latitude: 6.2442,
        Longitude: -75.5812,
        VisitedOn: new DateOnly(2025, 12, 1),
        Notes: "Viaje de fin de ano",
        ShortDescription: null,
        TypicalFood: "Bandeja paisa");
}
