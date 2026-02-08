using System.Text.RegularExpressions;

namespace Domain.ValueObject;

public sealed record VideoUrl
{
    private static readonly Regex _youTubeWatchUrlRegex = new(
        @"^https?://(www\.)?youtube\.com/watch\?v=[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex _youTubeShortsUrlRegex = new(
        @"^https?://(www\.)?youtube\.com/shorts/[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex _instagramUrlRegex = new(
        @"^https?://(www\.)?instagram\.com/p/[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex _videoIdRegex = new(
        @"[?&]v=([\w-]+)",
        RegexOptions.Compiled
    );

    public string Value { get; }
    public string? VideoId { get; }

    private VideoUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Video URL cannot be empty", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Video URL format is invalid", nameof(value));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Video URL format is invalid", nameof(value));
        }

        var host = uri.Host.ToLowerInvariant();
        var isYouTube = host == "youtube.com" || host == "www.youtube.com";
        var isInstagram = host == "instagram.com" || host == "www.instagram.com";

        if (!isYouTube && !isInstagram)
        {
            throw new ArgumentException(
                "URL must be from a supported platform (YouTube, Instagram)",
                nameof(value));
        }

        // Validate URL format based on platform
        var isValidFormat = _youTubeWatchUrlRegex.IsMatch(trimmedValue) ||
                           _youTubeShortsUrlRegex.IsMatch(trimmedValue) ||
                           _instagramUrlRegex.IsMatch(trimmedValue);

        if (!isValidFormat)
        {
            throw new ArgumentException(
                "Video URL must match a supported format: " +
                "YouTube (https://www.youtube.com/watch?v=*), " +
                "YouTube Shorts (https://www.youtube.com/shorts/*), " +
                "Instagram (https://www.instagram.com/p/*)",
                nameof(value));
        }

        Value = trimmedValue;

        // Extract video ID for YouTube watch URLs
        var match = _videoIdRegex.Match(trimmedValue);
        VideoId = match.Success ? match.Groups[1].Value : null;
    }

    public static VideoUrl Create(string value) => new(value);
}
