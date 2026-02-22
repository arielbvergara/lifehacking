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
}
