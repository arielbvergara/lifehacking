using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class VideoUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=dQw4w9WgXcQ")]
    public void Create_ShouldCreateVideoUrlWithExpectedValue_WhenValidYouTubeUrlProvided(string validUrl)
    {
        // Act
        var videoUrl = VideoUrl.Create(validUrl);

        // Assert
        videoUrl.Should().NotBeNull();
        videoUrl.Value.Should().Be(validUrl);
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/shorts/abc123")]
    [InlineData("http://www.youtube.com/shorts/test-video")]
    public void Create_ShouldCreateVideoUrlWithExpectedValue_WhenValidYouTubeShortsUrlProvided(string validUrl)
    {
        // Act
        var videoUrl = VideoUrl.Create(validUrl);

        // Assert
        videoUrl.Should().NotBeNull();
        videoUrl.Value.Should().Be(validUrl);
    }

    [Theory]
    [InlineData("https://www.instagram.com/p/ABC123xyz")]
    [InlineData("https://instagram.com/p/test-post")]
    [InlineData("http://www.instagram.com/p/another-post")]
    public void Create_ShouldCreateVideoUrlWithExpectedValue_WhenValidInstagramUrlProvided(string validUrl)
    {
        // Act
        var videoUrl = VideoUrl.Create(validUrl);

        // Assert
        videoUrl.Should().NotBeNull();
        videoUrl.Value.Should().Be(validUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNullOrWhitespaceProvided(string? invalidUrl)
    {
        // Act
        var act = () => VideoUrl.Create(invalidUrl!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Video URL cannot be empty*");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("htp://invalid")]
    public void Create_ShouldThrowArgumentException_WhenInvalidUrlFormat(string invalidUrl)
    {
        // Act
        var act = () => VideoUrl.Create(invalidUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Video URL format is invalid*");
    }

    [Theory]
    [InlineData("https://www.google.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://twitter.com/user/status/123")]
    public void Create_ShouldThrowArgumentException_WhenUnsupportedDomain(string unsupportedUrl)
    {
        // Act
        var act = () => VideoUrl.Create(unsupportedUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*URL must be from a supported platform*");
    }

    [Theory]
    [InlineData("https://www.youtube.com/")]
    [InlineData("https://www.youtube.com/channel/UCtest")]
    [InlineData("https://www.instagram.com/")]
    [InlineData("https://www.instagram.com/username/")]
    public void Create_ShouldThrowArgumentException_WhenInvalidFormatForPlatform(string invalidFormatUrl)
    {
        // Act
        var act = () => VideoUrl.Create(invalidFormatUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Video URL must match a supported format*");
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=abc123XYZ", "abc123XYZ")]
    [InlineData("https://www.youtube.com/watch?v=test-video_ID", "test-video_ID")]
    public void Create_ShouldExtractVideoId_WhenValidYouTubeWatchUrl(string url, string expectedVideoId)
    {
        // Act
        var videoUrl = VideoUrl.Create(url);

        // Assert
        videoUrl.VideoId.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.instagram.com/p/ABC123xyz")]
    public void Create_ShouldNotExtractVideoId_WhenNotYouTubeWatchUrl(string url)
    {
        // Act
        var videoUrl = VideoUrl.Create(url);

        // Assert
        videoUrl.VideoId.Should().BeNull();
    }

    [Fact]
    public void VideoUrl_ShouldHaveValueEquality()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var videoUrl1 = VideoUrl.Create(url);
        var videoUrl2 = VideoUrl.Create(url);

        // Act & Assert
        videoUrl1.Should().Be(videoUrl2);
        (videoUrl1 == videoUrl2).Should().BeTrue();
    }
}
