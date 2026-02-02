using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class YouTubeUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=dQw4w9WgXcQ")]
    public void Create_ShouldCreateYouTubeUrlWithExpectedValue_WhenValidUrlProvided(string validUrl)
    {
        // Act
        var youtubeUrl = YouTubeUrl.Create(validUrl);

        // Assert
        youtubeUrl.Should().NotBeNull();
        youtubeUrl.Value.Should().Be(validUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNullOrWhitespaceProvided(string? invalidUrl)
    {
        // Act
        var act = () => YouTubeUrl.Create(invalidUrl!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*YouTube URL cannot be empty*");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("htp://invalid")]
    public void Create_ShouldThrowArgumentException_WhenInvalidUrlFormat(string invalidUrl)
    {
        // Act
        var act = () => YouTubeUrl.Create(invalidUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*YouTube URL format is invalid*");
    }

    [Theory]
    [InlineData("https://www.google.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    public void Create_ShouldThrowArgumentException_WhenNonYouTubeDomain(string nonYouTubeUrl)
    {
        // Act
        var act = () => YouTubeUrl.Create(nonYouTubeUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*URL must be from youtube.com domain*");
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=abc123XYZ", "abc123XYZ")]
    [InlineData("https://www.youtube.com/watch?v=test-video_ID", "test-video_ID")]
    public void Create_ShouldExtractVideoId_WhenValidWatchUrl(string url, string expectedVideoId)
    {
        // Act
        var youtubeUrl = YouTubeUrl.Create(url);

        // Assert
        youtubeUrl.VideoId.Should().Be(expectedVideoId);
    }

    [Fact]
    public void YouTubeUrl_ShouldHaveValueEquality()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var youtubeUrl1 = YouTubeUrl.Create(url);
        var youtubeUrl2 = YouTubeUrl.Create(url);

        // Act & Assert
        youtubeUrl1.Should().Be(youtubeUrl2);
        (youtubeUrl1 == youtubeUrl2).Should().BeTrue();
    }
}
