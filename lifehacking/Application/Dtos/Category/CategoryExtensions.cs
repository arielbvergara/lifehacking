namespace Application.Dtos.Category;

public static class CategoryExtensions
{
    public static CategoryResponse ToCategoryResponse(this Domain.Entities.Category category, int tipCount = 0)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryResponse(
            category.Id.Value,
            category.Name,
            category.CreatedAt,
            category.UpdatedAt,
            category.Image?.ToImageDto(),
            tipCount
        );
    }

    public static ImageDto ToImageDto(this Domain.ValueObject.ImageMetadata image)
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

    public static Domain.ValueObject.ImageMetadata? ToImageMetadata(this ImageDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return Domain.ValueObject.ImageMetadata.Create(
            dto.ImageUrl,
            dto.ImageStoragePath,
            dto.OriginalFileName,
            dto.ContentType,
            dto.FileSizeBytes,
            dto.UploadedAt
        );
    }
}
