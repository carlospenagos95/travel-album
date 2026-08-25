using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CityMusicTests
{
    private const string StreamUrl = "http://stream.ejemplo.invalido/paisa.mp3";

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

        var video = city.AddYouTubeVideo("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Cumbia", Now);

        Assert.True(video.IsSuccess);
        Assert.False(video.Value.IsDefault);
        Assert.Equal(radio.Id, city.DefaultMusicSource!.Id);
    }

    [Fact]
    public void Marcar_otra_como_predeterminada_desmarca_la_anterior()
    {
        var city = NewCity();
        city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now);
        var video = city.AddYouTubeVideo("dQw4w9WgXcQ", null, Now).Value;

        var result = city.SetDefaultMusicSource(video.Id, Now);

        Assert.True(result.IsSuccess);
        Assert.Single(city.MusicSources, source => source.IsDefault);
        Assert.Equal(video.Id, city.DefaultMusicSource!.Id);
    }

    [Fact]
    public void Al_quitar_la_que_sonaba_otra_toma_el_relevo()
    {
        var city = NewCity();
        var radio = city.AddRadioStation("uuid-1", "Radio Paisa", StreamUrl, Now).Value;
        city.AddYouTubeVideo("dQw4w9WgXcQ", null, Now);

        Assert.True(city.RemoveMusicSource(radio.Id, Now).IsSuccess);
        Assert.NotNull(city.DefaultMusicSource);
    }

    [Fact]
    public void Sin_titulo_el_video_se_etiqueta_con_su_identificador()
    {
        var city = NewCity();

        var video = city.AddYouTubeVideo("https://youtu.be/dQw4w9WgXcQ", "   ", Now);

        Assert.True(video.IsSuccess);
        Assert.Equal("dQw4w9WgXcQ", video.Value.Label);
        Assert.Equal(MusicKind.YouTube, video.Value.Kind);
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
