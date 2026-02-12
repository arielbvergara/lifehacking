namespace Domain.Constants;

/// <summary>
/// Defines validation constants for category image metadata.
/// </summary>
public static class ImageConstants
{
    /// <summary>
    /// Allowed MIME types for category images.
    /// </summary>
    public static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    /// <summary>
    /// Maximum file size in bytes (5MB).
    /// </summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum length for image URL.
    /// </summary>
    public const int MaxUrlLength = 2048;

    /// <summary>
    /// Maximum length for storage path.
    /// </summary>
    public const int MaxStoragePathLength = 1024;

    /// <summary>
    /// Maximum length for original file name.
    /// </summary>
    public const int MaxFileNameLength = 255;

    /// <summary>
    /// Minimum image width in pixels (for future use).
    /// </summary>
    public const int MinWidth = 100;

    /// <summary>
    /// Maximum image width in pixels (for future use).
    /// </summary>
    public const int MaxWidth = 4096;

    /// <summary>
    /// Minimum image height in pixels (for future use).
    /// </summary>
    public const int MinHeight = 100;

    /// <summary>
    /// Maximum image height in pixels (for future use).
    /// </summary>
    public const int MaxHeight = 4096;
}
