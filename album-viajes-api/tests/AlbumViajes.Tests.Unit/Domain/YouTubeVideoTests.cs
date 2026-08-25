using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Tests.Unit.Domain;

public sealed class YouTubeVideoTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=42s")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("  https://m.youtube.com/watch?v=dQw4w9WgXcQ  ")]
    public void Reconoce_las_formas_en_que_se_comparte_un_video(string input)
    {
        var result = YouTubeVideo.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("dQw4w9WgXcQ", result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://www.youtube.com/watch?v=no-es-un-id-valido")]
    public void Rechaza_lo_que_no_identifica_un_video(string input)
    {
        var result = YouTubeVideo.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal("music.youTube", result.Error!.Code);
    }
}
