using Amazon.S3;
using Amazon.S3.Model;
using Application.Exceptions;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// AWS S3 implementation of the image storage service.
/// Uploads images to S3 with unique GUID-based filenames and generates CloudFront CDN URLs.
/// </summary>
public class S3ImageStorageService(
    IAmazonS3 s3Client,
    IOptions<AwsS3Options>? s3Options,
    IOptions<AwsCloudFrontOptions>? cloudFrontOptions,
    ILogger<S3ImageStorageService> logger)
    : IImageStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private readonly AwsS3Options _s3Options = s3Options?.Value ?? throw new ArgumentNullException(nameof(s3Options));
    private readonly AwsCloudFrontOptions _cloudFrontOptions = cloudFrontOptions?.Value ?? throw new ArgumentNullException(nameof(cloudFrontOptions));
    private readonly ILogger<S3ImageStorageService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<ImageStorageResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        try
        {
            // Generate unique storage path: categories/{year}/{month}/{guid}.{extension}
            var storagePath = GenerateStoragePath(originalFileName);

            _logger.LogInformation(
                "Uploading image to S3. Bucket: {BucketName}, Key: {StoragePath}, ContentType: {ContentType}",
                _s3Options.BucketName,
                storagePath,
                contentType);

            // Create S3 upload request
            var putRequest = new PutObjectRequest
            {
                BucketName = _s3Options.BucketName,
                Key = storagePath,
                InputStream = fileStream,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                Headers =
                {
                    CacheControl = "public, max-age=31536000" // 1 year cache for immutable images
                }
            };

            // Upload to S3
            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded image to S3. Key: {StoragePath}, ETag: {ETag}",
                storagePath,
                response.ETag);

            // Generate CloudFront URL
            var publicUrl = GenerateCloudFrontUrl(storagePath);

            return new ImageStorageResult(storagePath, publicUrl);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(
                ex,
                "S3 upload failed. Bucket: {BucketName}, ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                _s3Options.BucketName,
                ex.ErrorCode,
                ex.StatusCode);

            throw new InfraException("S3", "Failed to upload image to storage", ex);
        }
        catch (Exception ex) when (ex is not InfraException)
        {
            _logger.LogError(
                ex,
                "Unexpected error during S3 upload. Bucket: {BucketName}",
                _s3Options.BucketName);

            throw new InfraException("S3", "An unexpected error occurred during image upload", ex);
        }
    }

    /// <summary>
    /// Generates a unique storage path for the image.
    /// Format: categories/{year}/{month}/{guid}.{extension}
    /// </summary>
    /// <param name="originalFileName">The original filename to extract the extension from.</param>
    /// <returns>The generated storage path.</returns>
    private static string GenerateStoragePath(string originalFileName)
    {
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month.ToString("D2"); // Zero-padded month (01-12)
        var guid = Guid.NewGuid().ToString();
        var extension = Path.GetExtension(originalFileName).TrimStart('.');

        // Ensure extension is not empty
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = "jpg"; // Default extension if none provided
        }

        return $"public/categories/{year}/{month}/{guid}.{extension}";
    }

    /// <summary>
    /// Generates a CloudFront CDN URL for the uploaded image.
    /// Format: https://{domain}/{storagePath}
    /// </summary>
    /// <param name="storagePath">The S3 storage path (key).</param>
    /// <returns>The CloudFront URL.</returns>
    private string GenerateCloudFrontUrl(string storagePath)
    {
        return $"https://{_cloudFrontOptions.Domain}/{storagePath}";
    }
}
