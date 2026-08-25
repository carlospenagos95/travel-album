using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class CityPhotosTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Las_fotos_se_numeran_en_el_orden_en_que_entran()
    {
        var city = NewCity();

        var first = AddPhoto(city, "media-1");
        var second = AddPhoto(city, "media-2");

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal(first.Id, city.CoverPhoto!.Id);
    }

    [Fact]
    public void La_misma_foto_de_origen_no_entra_dos_veces()
    {
        var city = NewCity();
        AddPhoto(city, "media-1");

        var repeated = city.AddPhoto("media-1", "otra.jpg", "a/b.jpg", "a/b_thumb.jpg", 10, 10, null, Now);

        Assert.False(repeated.IsSuccess);
        Assert.Equal("city.photoDuplicate", repeated.Error!.Code);
        Assert.Single(city.Photos);
    }

    [Fact]
    public void Reordenar_cambia_la_portada()
    {
        var city = NewCity();
        var first = AddPhoto(city, "media-1");
        var second = AddPhoto(city, "media-2");

        var result = city.ReorderPhotos([second.Id, first.Id], Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(second.Id, city.CoverPhoto!.Id);
    }

    [Fact]
    public void Un_orden_incompleto_se_rechaza()
    {
        var city = NewCity();
        var first = AddPhoto(city, "media-1");
        AddPhoto(city, "media-2");

        var result = city.ReorderPhotos([first.Id], Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("photo.order", result.Error!.Code);
    }

    [Fact]
    public void Borrar_una_foto_cierra_el_hueco_que_deja_en_el_orden()
    {
        var city = NewCity();
        var first = AddPhoto(city, "media-1");
        var second = AddPhoto(city, "media-2");

        var removed = city.RemovePhoto(first.Id, Now);

        Assert.True(removed.IsSuccess);
        Assert.Equal(first.Id, removed.Value.Id);
        Assert.Equal(0, second.SortOrder);
    }

    [Fact]
    public void Borrar_una_foto_ajena_no_toca_la_galeria()
    {
        var city = NewCity();
        AddPhoto(city, "media-1");

        var removed = city.RemovePhoto(Guid.NewGuid(), Now);

        Assert.False(removed.IsSuccess);
        Assert.Equal("photo.notFound", removed.Error!.Code);
        Assert.Single(city.Photos);
    }

    [Fact]
    public void El_pie_de_foto_se_recorta_y_se_puede_quitar()
    {
        var city = NewCity();
        var photo = AddPhoto(city, "media-1");

        Assert.True(city.CaptionPhoto(photo.Id, "  Atardecer  ", Now).IsSuccess);
        Assert.Equal("Atardecer", photo.Caption);

        Assert.True(city.CaptionPhoto(photo.Id, "   ", Now).IsSuccess);
        Assert.Null(photo.Caption);
    }

    private static Photo AddPhoto(City city, string mediaId)
    {
        var added = city.AddPhoto(mediaId, $"{mediaId}.jpg", $"a/{mediaId}.jpg", $"a/{mediaId}_thumb.jpg", 800, 600, null, Now);

        Assert.True(added.IsSuccess);
        return added.Value;
    }

    private static City NewCity() => City.Create(
        "Medellin",
        "Colombia",
        CountryCode.Create("CO").Value,
        Coordinates.Create(6.2442, -75.5812).Value,
        Now).Value;
}
