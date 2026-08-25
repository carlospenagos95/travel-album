using System.Net;
using System.Net.Http.Json;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Tests.Integration;

public sealed class CityEndpointsTests(AlbumViajesApiFactory factory)
    : IClassFixture<AlbumViajesApiFactory>, IAsyncLifetime
{
    /// <summary>Cliente del duenio: solo el puede crear, editar y borrar.</summary>
    private readonly HttpClient _client = factory.CreateOwnerClient();

    public async Task InitializeAsync() => await factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_responde_ok()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Crear_una_ciudad_la_deja_disponible_en_la_lista_y_en_el_detalle()
    {
        var response = await _client.PostAsJsonAsync("/api/cities", Medellin());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CityDetailResponse>();
        Assert.NotNull(created);
        Assert.Equal("CO", created.CountryCode);
        Assert.NotEqual(Guid.Empty, created.Id);

        var list = await _client.GetFromJsonAsync<List<CitySummaryResponse>>("/api/cities");
        Assert.NotNull(list);
        Assert.Single(list);

        var detail = await _client.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Bandeja paisa", detail.TypicalFood);
    }

    [Fact]
    public async Task Registrar_dos_veces_la_misma_ciudad_devuelve_conflicto()
    {
        await _client.PostAsJsonAsync("/api/cities", Medellin());

        // Mismo nombre con otras mayusculas y espacios: sigue siendo la misma ciudad.
        var duplicate = await _client.PostAsJsonAsync("/api/cities", Medellin() with { Name = "  medellin " });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Las_coordenadas_fuera_de_rango_se_rechazan()
    {
        var response = await _client.PostAsJsonAsync("/api/cities", Medellin() with { Latitude = 120 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Actualizar_cambia_los_datos_y_conserva_la_fecha_de_creacion()
    {
        var created = await CreateAsync(Medellin());

        var response = await _client.PutAsJsonAsync(
            $"/api/cities/{created.Id}",
            Medellin() with { Name = "Bogota", Latitude = 4.7110, Longitude = -74.0721 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<CityDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Bogota", updated.Name);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Eliminar_la_quita_de_la_lista_y_el_segundo_intento_da_404()
    {
        var created = await CreateAsync(Medellin());

        var deleted = await _client.DeleteAsync($"/api/cities/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var again = await _client.DeleteAsync($"/api/cities/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);

        var list = await _client.GetFromJsonAsync<List<CitySummaryResponse>>("/api/cities");
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Un_visitante_ve_el_album_pero_no_puede_modificarlo()
    {
        var created = await CreateAsync(Medellin());
        var visitor = factory.CreateClient();

        var list = await visitor.GetAsync("/api/cities");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var detail = await visitor.GetAsync($"/api/cities/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var create = await visitor.PostAsJsonAsync("/api/cities", Medellin() with { Name = "Cali" });
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);

        var delete = await visitor.DeleteAsync($"/api/cities/{created.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task Pedir_una_ciudad_inexistente_devuelve_404()
    {
        var response = await _client.GetAsync($"/api/cities/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CityDetailResponse> CreateAsync(SaveCityRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/cities", request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CityDetailResponse>();
        Assert.NotNull(created);
        return created;
    }

    [Fact]
    public async Task Enriquecer_una_ciudad_guarda_los_datos_externos_en_la_ficha()
    {
        var created = await CreateAsync(Medellin());

        var response = await _client.PostAsync($"/api/cities/{created.Id}/enrich", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var enriched = await response.Content.ReadFromJsonAsync<CityDetailResponse>();
        Assert.NotNull(enriched);
        Assert.Equal("Q48278", enriched.WikidataId);
        Assert.NotNull(enriched.EnrichedAt);

        // Lo importante es que quedo persistido, no solo devuelto.
        var stored = await _client.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{created.Id}");
        Assert.NotNull(stored);
        Assert.Equal(2_427_129, stored.Population);
    }

    [Fact]
    public async Task Si_la_fuente_externa_falla_la_ficha_se_queda_como_estaba()
    {
        var created = await CreateAsync(Medellin());
        factory.Enricher.FailWith(Error.External("city.enrichmentUnavailable", "Sin respuesta."));

        var response = await _client.PostAsync($"/api/cities/{created.Id}/enrich", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var stored = await _client.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{created.Id}");
        Assert.NotNull(stored);
        Assert.Null(stored.EnrichedAt);
    }

    private static SaveCityRequest Medellin() => new(
        Name: "Medellin",
        Country: "Colombia",
        CountryCode: "co",
        Latitude: 6.2442,
        Longitude: -75.5812,
        VisitedOn: new DateOnly(2025, 12, 1),
        Notes: null,
        ShortDescription: null,
        TypicalFood: "Bandeja paisa");
}
