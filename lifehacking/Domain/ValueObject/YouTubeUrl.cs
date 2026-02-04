using System.Text.RegularExpressions;

namespace Domain.ValueObject;

public sealed record YouTubeUrl
{
    public const int MaxLength = 500;

    private static readonly Regex _youTubeUrlRegex = new(
        @"^https?://(www\.)?youtube\.com/watch\?v=[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex _videoIdRegex = new(
        @"[?&]v=([\w-]+)",
        RegexOptions.Compiled
    );

    public string Value { get; }
    public string? VideoId { get; }

    private YouTubeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("YouTube URL cannot be empty", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("YouTube URL format is invalid", nameof(value));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("YouTube URL format is invalid", nameof(value));
        }

        var host = uri.Host.ToLowerInvariant();
        if (host != "youtube.com" && host != "www.youtube.com")
        {
            throw new ArgumentException("URL must be from youtube.com domain", nameof(value));
        }

        if (!_youTubeUrlRegex.IsMatch(trimmedValue))
        {
            throw new ArgumentException("YouTube URL must be a valid watch URL", nameof(value));
        }

        Value = trimmedValue;

        var match = _videoIdRegex.Match(trimmedValue);
        VideoId = match.Success ? match.Groups[1].Value : null;
    }

    public static YouTubeUrl Create(string value) => new(value);
}
