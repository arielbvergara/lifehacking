using Domain.Constants;

namespace Domain.ValueObject;

/// <summary>
/// Value object representing image metadata for a category.
/// Encapsulates all image-related properties and validation logic.
/// </summary>
public sealed record CategoryImage
{
    /// <summary>
    /// The public-facing URL for accessing the image (CloudFront/S3).
    /// </summary>
    public string ImageUrl { get; }

    /// <summary>
    /// The full storage path to the image file in AWS S3.
    /// </summary>
    public string ImageStoragePath { get; }

    /// <summary>
    /// The original filename of the uploaded image.
    /// </summary>
    public string OriginalFileName { get; }

    /// <summary>
    /// The MIME type of the image (e.g., "image/jpeg").
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; }

    /// <summary>
    /// The timestamp when the image was uploaded (UTC).
    /// </summary>
    public DateTime UploadedAt { get; }

    private CategoryImage(
        string imageUrl,
        string imageStoragePath,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        DateTime uploadedAt)
    {
        ImageUrl = imageUrl;
        ImageStoragePath = imageStoragePath;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        UploadedAt = uploadedAt;
    }

    /// <summary>
    /// Creates a new CategoryImage with validation.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any validation rule fails.</exception>
    public static CategoryImage Create(
        string imageUrl,
        string imageStoragePath,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        DateTime uploadedAt)
    {
        ValidateImageUrl(imageUrl);
        ValidateImageStoragePath(imageStoragePath);
        ValidateOriginalFileName(originalFileName);
        ValidateContentType(contentType);
        ValidateFileSizeBytes(fileSizeBytes);

        return new CategoryImage(
            imageUrl,
            imageStoragePath,
            originalFileName,
            contentType,
            fileSizeBytes,
            uploadedAt);
    }

    private static void ValidateImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be empty", nameof(imageUrl));
        }

        if (imageUrl.Length > ImageConstants.MaxUrlLength)
        {
            throw new ArgumentException(
                $"Image URL cannot exceed {ImageConstants.MaxUrlLength} characters",
                nameof(imageUrl));
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Image URL must be a valid absolute URL", nameof(imageUrl));
        }
    }

    private static void ValidateImageStoragePath(string imageStoragePath)
    {
        if (string.IsNullOrWhiteSpace(imageStoragePath))
        {
            throw new ArgumentException("Image storage path cannot be empty", nameof(imageStoragePath));
        }

        if (imageStoragePath.Length > ImageConstants.MaxStoragePathLength)
        {
            throw new ArgumentException(
                $"Image storage path cannot exceed {ImageConstants.MaxStoragePathLength} characters",
                nameof(imageStoragePath));
        }
    }

    private static void ValidateOriginalFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be empty", nameof(originalFileName));
        }

        if (originalFileName.Length > ImageConstants.MaxFileNameLength)
        {
            throw new ArgumentException(
                $"Original file name cannot exceed {ImageConstants.MaxFileNameLength} characters",
                nameof(originalFileName));
        }
    }

    private static void ValidateContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type cannot be empty", nameof(contentType));
        }

        if (!ImageConstants.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            var allowedTypes = string.Join(", ", ImageConstants.AllowedContentTypes);
            throw new ArgumentException(
                $"Content type must be one of: {allowedTypes}",
                nameof(contentType));
        }
    }

    private static void ValidateFileSizeBytes(long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero", nameof(fileSizeBytes));
        }

        if (fileSizeBytes > ImageConstants.MaxFileSizeBytes)
        {
            throw new ArgumentException(
                $"File size cannot exceed {ImageConstants.MaxFileSizeBytes} bytes ({ImageConstants.MaxFileSizeBytes / (1024 * 1024)}MB)",
                nameof(fileSizeBytes));
        }
    }
}
