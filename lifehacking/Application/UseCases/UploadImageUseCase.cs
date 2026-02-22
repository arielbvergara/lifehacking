using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validation;
using Domain.Constants;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.UseCases;

/// <summary>
/// Use case for uploading images to cloud storage.
/// Validates image files and uploads them to AWS S3 with CloudFront CDN URLs.
/// </summary>
public class UploadImageUseCase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<UploadImageUseCase> _logger;

    public UploadImageUseCase(
        IImageStorageService imageStorageService,
        ILogger<UploadImageUseCase> logger)
    {
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the use case to upload an image.
    /// </summary>
    /// <param name="fileStream">The stream containing the image file data.</param>
    /// <param name="fileName">The original filename of the uploaded image.</param>
    /// <param name="contentType">The declared content type of the image.</param>
    /// <param name="fileSizeBytes">The size of the file in bytes.</param>
    /// <param name="pathPrefix">The storage path prefix (e.g., "categories", "tips").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the image metadata or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="ValidationException"/> if the file stream is null.</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> if the file size exceeds 5MB.</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> if the content type is not in the allowed list.</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> if the magic bytes do not match the declared content type.</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if the S3 upload fails.</description></item>
    /// </list>
    /// </remarks>
    public async Task<Result<ImageDto, AppException>> ExecuteAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string pathPrefix = "categories",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationBuilder = new ValidationErrorBuilder();

            // Validate file stream is not null
            if (fileStream == null)
            {
                validationBuilder.AddError("File", "File is required");
                return Result<ImageDto, AppException>.Fail(validationBuilder.Build());
            }

            // Validate file size
            if (fileSizeBytes > ImageConstants.MaxFileSizeBytes)
            {
                var maxSizeMB = ImageConstants.MaxFileSizeBytes / (1024 * 1024);
                validationBuilder.AddError("File", $"File size cannot exceed {maxSizeMB}MB");
            }

            // Validate content type
            var normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
            if (!ImageConstants.AllowedContentTypes.Contains(normalizedContentType))
            {
                var allowedTypes = string.Join(", ", ImageConstants.AllowedContentTypes);
                validationBuilder.AddError("File", $"Content type must be one of: {allowedTypes}");
            }

            // Validate magic bytes (only if content type is valid)
            if (!validationBuilder.HasErrors)
            {
                if (!FileValidationHelper.ValidateMagicBytes(fileStream, normalizedContentType))
                {
                    validationBuilder.AddError("File", "File format does not match the declared content type");
                }
            }

            // Return early if validation errors exist
            if (validationBuilder.HasErrors)
            {
                return Result<ImageDto, AppException>.Fail(validationBuilder.Build());
            }

            // Sanitize filename
            var sanitizedFileName = FileValidationHelper.SanitizeFileName(fileName);

            _logger.LogInformation(
                "Uploading {PathPrefix} image. OriginalFileName: {OriginalFileName}, SanitizedFileName: {SanitizedFileName}, ContentType: {ContentType}, Size: {Size} bytes",
                pathPrefix,
                fileName,
                sanitizedFileName,
                normalizedContentType,
                fileSizeBytes);

            // Upload to storage
            var storageResult = await _imageStorageService.UploadAsync(
                fileStream,
                sanitizedFileName,
                normalizedContentType,
                pathPrefix,
                cancellationToken);

            // Create response DTO
            var imageDto = new ImageDto(
                ImageUrl: storageResult.PublicUrl,
                ImageStoragePath: storageResult.StoragePath,
                OriginalFileName: sanitizedFileName,
                ContentType: normalizedContentType,
                FileSizeBytes: fileSizeBytes,
                UploadedAt: DateTime.UtcNow
            );

            _logger.LogInformation(
                "Successfully uploaded {PathPrefix} image. StoragePath: {StoragePath}, PublicUrl: {PublicUrl}",
                pathPrefix,
                storageResult.StoragePath,
                storageResult.PublicUrl);

            return Result<ImageDto, AppException>.Ok(imageDto);
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "Application error during {PathPrefix} image upload", pathPrefix);
            return Result<ImageDto, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {PathPrefix} image upload", pathPrefix);
            return Result<ImageDto, AppException>.Fail(
                new InfraException($"An unexpected error occurred while uploading the {pathPrefix} image", ex));
        }
    }
}
