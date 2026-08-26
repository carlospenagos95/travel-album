using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CityMusicTests
{
    private const string StreamUrl = "http://stream.ejemplo.invalido/paisa.mp3";
    private const string TrackUrl = "https://prod-1.storage.jamendo.invalido/1532771.mp3";

    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void La_primera_fuente_queda_marcada_como_la_que_suena()
    {
        var city = NewCity();

        var radio = city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now);

        Assert.True(radio.IsSuccess);
        Assert.True(radio.Value.IsDefault);
        Assert.Equal(radio.Value.Id, city.DefaultMusicSource!.Id);
    }

    [Fact]
    public void Las_siguientes_fuentes_no_desplazan_a_la_que_ya_sonaba()
    {
        var city = NewCity();
        var radio = city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now).Value;

        var track = city.AddTrack("1532771", "Cumbia", "Los Ejemplos", TrackUrl, Now);

        Assert.True(track.IsSuccess);
        Assert.False(track.Value.IsDefault);
        Assert.Equal(radio.Id, city.DefaultMusicSource!.Id);
    }

    [Fact]
    public void Marcar_otra_como_predeterminada_desmarca_la_anterior()
    {
        var city = NewCity();
        city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now);
        var track = city.AddTrack("1532771", "Cumbia", null, TrackUrl, Now).Value;

        var result = city.SetDefaultMusicSource(track.Id, Now);

        Assert.True(result.IsSuccess);
        Assert.Single(city.MusicSources, source => source.IsDefault);
        Assert.Equal(track.Id, city.DefaultMusicSource!.Id);
    }

    [Fact]
    public void Al_quitar_la_que_sonaba_otra_toma_el_relevo()
    {
        var city = NewCity();
        var radio = city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now).Value;
        city.AddTrack("1532771", "Cumbia", null, TrackUrl, Now);

        Assert.True(city.RemoveMusicSource(radio.Id, Now).IsSuccess);
        Assert.NotNull(city.DefaultMusicSource);
    }

    [Fact]
    public void Una_cancion_guarda_su_artista_y_su_audio()
    {
        var city = NewCity();

        var track = city.AddTrack("1532771", "Cumbia", "  Los Ejemplos  ", TrackUrl, Now);

        Assert.True(track.IsSuccess);
        Assert.Equal("Cumbia", track.Value.Label);
        Assert.Equal("Los Ejemplos", track.Value.TrackArtist);
        Assert.Equal(TrackUrl, track.Value.TrackAudioUrl);
        Assert.Equal(MusicKind.Track, track.Value.Kind);
    }

    [Fact]
    public void Una_cancion_sin_titulo_se_rechaza()
    {
        var city = NewCity();

        var track = city.AddTrack("1532771", "   ", null, TrackUrl, Now);

        Assert.False(track.IsSuccess);
        Assert.Equal("music.trackTitle", track.Error!.Code);
        Assert.Empty(city.MusicSources);
    }

    [Fact]
    public void Una_cancion_sin_audio_valido_se_rechaza()
    {
        var city = NewCity();

        var track = city.AddTrack("1532771", "Cumbia", null, "ftp://ejemplo.invalido/x", Now);

        Assert.False(track.IsSuccess);
        Assert.Equal("music.streamUrl", track.Error!.Code);
        Assert.Empty(city.MusicSources);
    }

    [Fact]
    public void Una_emisora_sin_direccion_valida_se_rechaza()
    {
        var city = NewCity();

        var radio = city.AddRadioStation("uuid-1", "Radio Paisa", "ftp://ejemplo.invalido/x", Now);

        Assert.False(radio.IsSuccess);
        Assert.Equal("music.streamUrl", radio.Error!.Code);
        Assert.Empty(city.MusicSources);
    }

    private static City NewCity() => City.Create(
        "Medellin",
        "Colombia",
        CountryCode.Create("CO").Value,
        Coordinates.Create(6.2442, -75.5812).Value,
        Now).Value;
}
