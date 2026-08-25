using System.Net;
using System.Net.Http.Json;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Application.Music.Contracts;

namespace AlbumViajes.Tests.Integration;

public sealed class MusicEndpointsTests(AlbumViajesApiFactory factory)
    : IClassFixture<AlbumViajesApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _owner = factory.CreateOwnerClient();

    public async Task InitializeAsync() => await factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task La_primera_fuente_que_se_agrega_es_la_que_suena()
    {
        var city = await CreateCityAsync();

        var radio = await AddRadioAsync(city.Id);
        Assert.True(radio.IsDefault);
        Assert.Equal("RadioStation", radio.Kind);

        var youTube = await AddYouTubeAsync(city.Id, "https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        Assert.False(youTube.IsDefault);
        Assert.Equal("dQw4w9WgXcQ", youTube.YouTubeVideoId);
    }

    [Fact]
    public async Task El_enlace_de_reproduccion_apunta_al_servidor_y_no_a_la_emisora()
    {
        var city = await CreateCityAsync();
        var radio = await AddRadioAsync(city.Id);

        Assert.Equal($"/api/cities/{city.Id}/music/{radio.Id}/stream", radio.StreamUrl);

        // Publico: quien ve el album oye la ciudad sin identificarse.
        var visitor = factory.CreateClient();
        var stream = await visitor.GetAsync(radio.StreamUrl);

        Assert.Equal(HttpStatusCode.OK, stream.StatusCode);
        Assert.Equal(StubAudioStreamReader.Sample, await stream.Content.ReadAsStringAsync());
        Assert.Equal(StubRadioDirectory.Station.StreamUrl, factory.AudioStreamReader.LastRequestedUrl?.ToString());
    }

    [Fact]
    public async Task Un_video_de_youtube_no_se_reproduce_por_el_proxy()
    {
        var city = await CreateCityAsync();
        var youTube = await AddYouTubeAsync(city.Id, "dQw4w9WgXcQ");

        var response = await _owner.GetAsync($"/api/cities/{city.Id}/music/{youTube.Id}/stream");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cambiar_la_fuente_por_defecto_deja_solo_una_marcada()
    {
        var city = await CreateCityAsync();
        await AddRadioAsync(city.Id);
        var youTube = await AddYouTubeAsync(city.Id, "https://youtu.be/dQw4w9WgXcQ");

        var response = await _owner.PutAsync($"/api/cities/{city.Id}/music/{youTube.Id}/default", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var playlist = await response.Content.ReadFromJsonAsync<List<MusicSourceResponse>>();
        Assert.NotNull(playlist);
        Assert.Single(playlist, source => source.IsDefault);
        Assert.Equal(youTube.Id, playlist[0].Id);
    }

    [Fact]
    public async Task Al_quitar_la_fuente_por_defecto_otra_toma_el_relevo()
    {
        var city = await CreateCityAsync();
        var radio = await AddRadioAsync(city.Id);
        await AddYouTubeAsync(city.Id, "dQw4w9WgXcQ");

        var deleted = await _owner.DeleteAsync($"/api/cities/{city.Id}/music/{radio.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var detail = await _owner.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{city.Id}");
        Assert.NotNull(detail);
        Assert.Single(detail.MusicSources);
        Assert.True(detail.MusicSources[0].IsDefault);
    }

    [Fact]
    public async Task Un_enlace_que_no_es_de_youtube_se_rechaza()
    {
        var city = await CreateCityAsync();

        var response = await _owner.PostAsJsonAsync(
            $"/api/cities/{city.Id}/music/youtube",
            new AddYouTubeVideoRequest("https://ejemplo.invalido/cancion", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Buscar_emisoras_requiere_ser_el_duenio()
    {
        var visitor = factory.CreateClient();

        var response = await visitor.GetAsync("/api/stations/search?query=medellin");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var found = await _owner.GetFromJsonAsync<List<RadioStationResponse>>(
            "/api/stations/search?query=medellin&countryCode=CO");

        Assert.NotNull(found);
        Assert.Equal(StubRadioDirectory.Station.Uuid, found[0].Uuid);
    }

    private async Task<MusicSourceResponse> AddRadioAsync(Guid cityId)
    {
        var station = StubRadioDirectory.Station;

        var response = await _owner.PostAsJsonAsync(
            $"/api/cities/{cityId}/music/radio",
            new AddRadioStationRequest(station.Uuid, station.Name, station.StreamUrl));

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<MusicSourceResponse>();
        Assert.NotNull(created);

        return created;
    }

    private async Task<MusicSourceResponse> AddYouTubeAsync(Guid cityId, string urlOrId)
    {
        var response = await _owner.PostAsJsonAsync(
            $"/api/cities/{cityId}/music/youtube",
            new AddYouTubeVideoRequest(urlOrId, "Cumbia del recuerdo"));

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<MusicSourceResponse>();
        Assert.NotNull(created);

        return created;
    }

    private async Task<CityDetailResponse> CreateCityAsync()
    {
        var response = await _owner.PostAsJsonAsync("/api/cities", new SaveCityRequest(
            Name: "Barranquilla",
            Country: "Colombia",
            CountryCode: "CO",
            Latitude: 10.9685,
            Longitude: -74.7813,
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
