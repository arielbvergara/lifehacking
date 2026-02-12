namespace Application.Dtos.Category;

public static class CategoryExtensions
{
    public static CategoryResponse ToCategoryResponse(this Domain.Entities.Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryResponse(
            category.Id.Value,
            category.Name,
            category.CreatedAt,
            category.UpdatedAt,
            category.Image?.ToCategoryImageDto()
        );
    }

    public static CategoryImageDto ToCategoryImageDto(this Domain.ValueObject.CategoryImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new CategoryImageDto(
            image.ImageUrl,
            image.ImageStoragePath,
            image.OriginalFileName,
            image.ContentType,
            image.FileSizeBytes,
            image.UploadedAt
        );
    }

    public static Domain.ValueObject.CategoryImage? ToCategoryImage(this CategoryImageDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return Domain.ValueObject.CategoryImage.Create(
            dto.ImageUrl,
            dto.ImageStoragePath,
            dto.OriginalFileName,
            dto.ContentType,
            dto.FileSizeBytes,
            dto.UploadedAt
        );
    }
}
