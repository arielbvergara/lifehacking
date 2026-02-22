using Domain.ValueObject;

namespace Application.Dtos;

/// <summary>
/// Extension methods for mapping between <see cref="ImageMetadata"/> and <see cref="ImageDto"/>.
/// </summary>
public static class ImageExtensions
{
    public static ImageDto ToImageDto(this ImageMetadata image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new ImageDto(
            image.ImageUrl,
            image.ImageStoragePath,
            image.OriginalFileName,
            image.ContentType,
            image.FileSizeBytes,
            image.UploadedAt
        );
    }

    public static ImageMetadata? ToImageMetadata(this ImageDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return ImageMetadata.Create(
            dto.ImageUrl,
            dto.ImageStoragePath,
            dto.OriginalFileName,
            dto.ContentType,
            dto.FileSizeBytes,
            dto.UploadedAt
        );
    }
}
