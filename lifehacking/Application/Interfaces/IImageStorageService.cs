namespace Application.Interfaces;

/// <summary>
/// Defines the contract for image storage operations.
/// Implementations handle uploading images to cloud storage (e.g., AWS S3) and generating public URLs.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Uploads an image file to cloud storage and returns the storage path and public URL.
    /// </summary>
    /// <param name="fileStream">The stream containing the image file data to upload.</param>
    /// <param name="originalFileName">The original filename of the uploaded image, used to extract the file extension.</param>
    /// <param name="contentType">The validated MIME content type of the image (e.g., image/jpeg, image/png).</param>
    /// <param name="pathPrefix">The path prefix for organizing images (e.g., "categories", "tips"). Defaults to "categories" for backward compatibility.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous upload operation.
    /// The task result contains an <see cref="ImageStorageResult"/> with the storage path and public URL.
    /// </returns>
    /// <exception cref="Application.Exceptions.InfraException">
    /// Thrown when the upload operation fails due to infrastructure issues (e.g., network errors, storage service unavailable).
    /// </exception>
    Task<ImageStorageResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        string pathPrefix = "categories",
        CancellationToken cancellationToken = default);
}
