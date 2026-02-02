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
            category.UpdatedAt
        );
    }
}
