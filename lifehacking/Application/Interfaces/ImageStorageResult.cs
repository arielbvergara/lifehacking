namespace Application.Interfaces;

/// <summary>
/// Represents the result of an image storage operation.
/// Contains the storage path (S3 key) and the public URL (CloudFront) for the uploaded image.
/// </summary>
/// <param name="StoragePath">The S3 storage path (key) for the uploaded image, e.g., categories/2025/01/abc-123.jpg</param>
/// <param name="PublicUrl">The public CloudFront URL for accessing the uploaded image, e.g., https://d123.cloudfront.net/categories/2025/01/abc-123.jpg</param>
public record ImageStorageResult(
    string StoragePath,
    string PublicUrl);
