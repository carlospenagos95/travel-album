using System.Net;
using System.Net.Http.Headers;
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

        var track = await AddTrackAsync(city.Id);
        Assert.False(track.IsDefault);
        Assert.Equal("Track", track.Kind);
        Assert.Equal(StubMusicLibrary.Track.ArtistName, track.Artist);
        Assert.Equal($"/api/cities/{city.Id}/music/{track.Id}/stream", track.StreamUrl);
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
    public async Task La_cancion_tambien_pasa_por_el_proxy_y_con_un_tipo_que_el_navegador_entiende()
    {
        // iTunes marca sus muestras como audio/x-m4p, que algunos navegadores no
        // reconocen aunque el fichero sea AAC corriente.
        factory.AudioStreamReader.ContentType = "audio/x-m4p";

        var city = await CreateCityAsync();
        var track = await AddTrackAsync(city.Id);

        // Publico: quien ve el album oye la ciudad sin identificarse.
        var visitor = factory.CreateClient();
        var response = await visitor.GetAsync($"/api/cities/{city.Id}/music/{track.Id}/stream");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("audio/mp4", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StubAudioStreamReader.Sample, await response.Content.ReadAsStringAsync());
        Assert.Equal(StubMusicLibrary.Track.AudioUrl, factory.AudioStreamReader.LastRequestedUrl?.ToString());

        // Un .m4a lleva el indice al final: sin poder pedir trozos sueltos, el
        // navegador no reproduce nada aunque la respuesta llegue entera.
        var partial = new HttpRequestMessage(HttpMethod.Get, $"/api/cities/{city.Id}/music/{track.Id}/stream");
        partial.Headers.Range = new RangeHeaderValue(0, 3);

        var chunk = await visitor.SendAsync(partial);

        Assert.Equal(HttpStatusCode.PartialContent, chunk.StatusCode);
        Assert.Equal(StubAudioStreamReader.Sample[..4], await chunk.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_radio_se_reenvia_segun_llega_y_no_admite_trozos()
    {
        var city = await CreateCityAsync();
        var radio = await AddRadioAsync(city.Id);

        var visitor = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, radio.StreamUrl);
        request.Headers.Range = new RangeHeaderValue(0, 3);

        // El stream de una emisora no termina, asi que no hay nada que recorrer:
        // se entrega entero y se ignora el rango.
        var response = await visitor.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StubAudioStreamReader.Sample, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cambiar_la_fuente_por_defecto_deja_solo_una_marcada()
    {
        var city = await CreateCityAsync();
        await AddRadioAsync(city.Id);
        var track = await AddTrackAsync(city.Id);

        var response = await _owner.PutAsync($"/api/cities/{city.Id}/music/{track.Id}/default", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var playlist = await response.Content.ReadFromJsonAsync<List<MusicSourceResponse>>();
        Assert.NotNull(playlist);
        Assert.Single(playlist, source => source.IsDefault);
        Assert.Equal(track.Id, playlist[0].Id);
    }

    [Fact]
    public async Task Al_quitar_la_fuente_por_defecto_otra_toma_el_relevo()
    {
        var city = await CreateCityAsync();
        var radio = await AddRadioAsync(city.Id);
        await AddTrackAsync(city.Id);

        var deleted = await _owner.DeleteAsync($"/api/cities/{city.Id}/music/{radio.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var detail = await _owner.GetFromJsonAsync<CityDetailResponse>($"/api/cities/{city.Id}");
        Assert.NotNull(detail);
        Assert.Single(detail.MusicSources);
        Assert.True(detail.MusicSources[0].IsDefault);
    }

    [Fact]
    public async Task Una_cancion_sin_audio_valido_se_rechaza()
    {
        var city = await CreateCityAsync();

        var response = await _owner.PostAsJsonAsync(
            $"/api/cities/{city.Id}/music/track",
            new AddTrackRequest("1532771", "Cumbia del recuerdo", "Los Ejemplos", "ftp://ejemplo.invalido/cancion"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Buscar_canciones_requiere_ser_el_duenio()
    {
        var visitor = factory.CreateClient();

        var response = await visitor.GetAsync("/api/tracks/search?query=cumbia");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var found = await _owner.GetFromJsonAsync<List<LibraryTrackResponse>>("/api/tracks/search?query=cumbia");

        Assert.NotNull(found);
        Assert.Equal(StubMusicLibrary.Track.Id, found[0].Id);
        Assert.Equal(StubMusicLibrary.Track.AudioUrl, found[0].AudioUrl);
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

    private async Task<MusicSourceResponse> AddTrackAsync(Guid cityId)
    {
        var track = StubMusicLibrary.Track;

        var response = await _owner.PostAsJsonAsync(
            $"/api/cities/{cityId}/music/track",
            new AddTrackRequest(track.Id, track.Name, track.ArtistName, track.AudioUrl));

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
