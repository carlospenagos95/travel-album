using System.Net;
using System.Net.Http.Json;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Application.Photos.Contracts;

namespace AlbumViajes.Tests.Integration;

public sealed class PhotoEndpointsTests(AlbumViajesApiFactory factory)
    : IClassFixture<AlbumViajesApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _owner = factory.CreateOwnerClient();

    public async Task InitializeAsync() => await factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Importar_copia_las_fotos_al_servidor_y_las_deja_en_la_ficha()
    {
        var city = await CreateCityAsync();

        var imported = await ImportAsync(city.Id);

        Assert.Equal(1, imported.Imported);
        Assert.Equal(0, imported.Skipped);
        Assert.Single(imported.Photos);
        Assert.True(factory.PhotoLibrary.SessionDiscarded);

        var detail = await _owner.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{city.Id}");
        Assert.NotNull(detail);
        Assert.Single(detail.Photos);
        Assert.Equal("playa.jpg", detail.Photos[0].FileName);
        Assert.Equal(800, detail.Photos[0].Width);
    }

    [Fact]
    public async Task La_foto_se_sirve_desde_el_servidor_y_no_desde_google()
    {
        var city = await CreateCityAsync();
        var imported = await ImportAsync(city.Id);
        var photo = imported.Photos[0];

        // Publico: quien ve el album no se identifica.
        var visitor = factory.CreateClient();

        var original = await visitor.GetAsync(photo.FileUrl);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal("image/jpeg", original.Content.Headers.ContentType?.MediaType);

        var thumbnail = await visitor.GetAsync(photo.ThumbnailUrl);
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);

        // La miniatura pesa menos que el original: se genero de verdad.
        var originalBytes = await original.Content.ReadAsByteArrayAsync();
        var thumbnailBytes = await thumbnail.Content.ReadAsByteArrayAsync();
        Assert.True(thumbnailBytes.Length < originalBytes.Length);
    }

    [Fact]
    public async Task Volver_a_elegir_la_misma_foto_no_la_duplica()
    {
        var city = await CreateCityAsync();
        await ImportAsync(city.Id);

        var again = await ImportAsync(city.Id);

        Assert.Equal(0, again.Imported);
        Assert.Equal(1, again.Skipped);
        Assert.Single(again.Photos);
    }

    [Fact]
    public async Task La_portada_de_la_ciudad_es_la_primera_foto_de_la_galeria()
    {
        var city = await CreateCityAsync();
        factory.PhotoLibrary.Picked.Add(StubPhotoLibrary.PhotoNamed("montania.jpg"));

        var imported = await ImportAsync(city.Id);
        Assert.Equal(2, imported.Imported);

        var reordered = await _owner.PutAsJsonAsync(
            $"/api/cities/{city.Id}/photos/order",
            new ReorderPhotosRequest([imported.Photos[1].Id, imported.Photos[0].Id]));

        Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);

        var list = await _owner.GetFromJsonAsync<List<CitySummaryResponse>>("/api/cities");
        Assert.NotNull(list);
        Assert.Equal(2, list[0].PhotoCount);
        Assert.Equal(imported.Photos[1].ThumbnailUrl, list[0].CoverThumbnailUrl);
    }

    [Fact]
    public async Task Borrar_una_foto_la_quita_de_la_ficha_y_del_disco()
    {
        var city = await CreateCityAsync();
        var imported = await ImportAsync(city.Id);
        var photo = imported.Photos[0];

        var deleted = await _owner.DeleteAsync($"/api/cities/{city.Id}/photos/{photo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var missing = await _owner.GetAsync(photo.FileUrl);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var remaining = Directory.Exists(factory.PhotoRoot)
            ? Directory.GetFiles(factory.PhotoRoot, "*", SearchOption.AllDirectories)
            : [];

        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Importar_antes_de_terminar_de_elegir_se_rechaza()
    {
        var city = await CreateCityAsync();
        factory.PhotoLibrary.PhotosPicked = false;

        var response = await _owner.PostAsync(
            $"/api/cities/{city.Id}/photos/import?sessionId={StubPhotoLibrary.SessionId}",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Un_visitante_no_puede_importar_ni_borrar_fotos()
    {
        var city = await CreateCityAsync();
        var imported = await ImportAsync(city.Id);
        var visitor = factory.CreateClient();

        var session = await visitor.PostAsync($"/api/cities/{city.Id}/photos/picker-session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);

        var delete = await visitor.DeleteAsync($"/api/cities/{city.Id}/photos/{imported.Photos[0].Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    private async Task<PhotoImportResponse> ImportAsync(Guid cityId)
    {
        var response = await _owner.PostAsync(
            $"/api/cities/{cityId}/photos/import?sessionId={StubPhotoLibrary.SessionId}",
            content: null);

        response.EnsureSuccessStatusCode();

        var imported = await response.Content.ReadFromJsonAsync<PhotoImportResponse>();
        Assert.NotNull(imported);

        return imported;
    }

    private async Task<CityDetailResponse> CreateCityAsync()
    {
        var response = await _owner.PostAsJsonAsync("/api/cities", new SaveCityRequest(
            Name: "Cartagena",
            Country: "Colombia",
            CountryCode: "CO",
            Latitude: 10.3910,
            Longitude: -75.4794,
            VisitedOn: null,
            Notes: null,
            ShortDescription: null,
            TypicalFood: null));

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CityDetailResponse>();
        Assert.NotNull(created);

        return created;
    }
}
