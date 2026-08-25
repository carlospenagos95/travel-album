using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Application.Cities.UseCases;
using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;
using NSubstitute;

namespace AlbumViajes.Tests.Unit.Application;

public sealed class UpdateAndDeleteCityHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly CountryCode Colombia = CountryCode.Create("CO").Value;

    private readonly ICityRepository _cities = Substitute.For<ICityRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Update_devuelve_not_found_cuando_la_ciudad_no_existe()
    {
        _cities.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((City?)null);
        var handler = new UpdateCityHandler(_cities, _unitOfWork, new FixedClock(Now));

        var result = await handler.HandleAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task Update_aplica_los_cambios_y_conserva_la_fecha_de_creacion()
    {
        var city = ExistingCity();
        _cities.FindAsync(city.Id, Arg.Any<CancellationToken>()).Returns(city);
        _cities.ExistsWithNameAsync("Bogota", Colombia, city.Id, Arg.Any<CancellationToken>()).Returns(false);
        var later = Now.AddDays(2);
        var handler = new UpdateCityHandler(_cities, _unitOfWork, new FixedClock(later));

        var request = ValidRequest() with { Name = "Bogota", Latitude = 4.7110, Longitude = -74.0721 };
        var result = await handler.HandleAsync(city.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bogota", result.Value.Name);
        Assert.Equal(4.7110, result.Value.Latitude);
        Assert.Equal(Now, result.Value.CreatedAt);
        Assert.Equal(later, result.Value.UpdatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ignora_la_propia_ciudad_al_buscar_duplicados()
    {
        var city = ExistingCity();
        _cities.FindAsync(city.Id, Arg.Any<CancellationToken>()).Returns(city);
        var handler = new UpdateCityHandler(_cities, _unitOfWork, new FixedClock(Now));

        await handler.HandleAsync(city.Id, ValidRequest(), CancellationToken.None);

        await _cities.Received(1).ExistsWithNameAsync("Medellin", Colombia, city.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_elimina_y_confirma()
    {
        var city = ExistingCity();
        _cities.FindAsync(city.Id, Arg.Any<CancellationToken>()).Returns(city);
        var handler = new DeleteCityHandler(_cities, _unitOfWork);

        var result = await handler.HandleAsync(city.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _cities.Received(1).Remove(city);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_devuelve_not_found_sin_tocar_la_transaccion()
    {
        _cities.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((City?)null);
        var handler = new DeleteCityHandler(_cities, _unitOfWork);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static City ExistingCity() => City.Create(
        "Medellin",
        "Colombia",
        Colombia,
        Coordinates.Create(6.2442, -75.5812).Value,
        Now).Value;

    private static SaveCityRequest ValidRequest() => new(
        Name: "Medellin",
        Country: "Colombia",
        CountryCode: "CO",
        Latitude: 6.2442,
        Longitude: -75.5812,
        VisitedOn: null,
        Notes: null,
        ShortDescription: null,
        TypicalFood: null);
}
