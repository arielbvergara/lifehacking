namespace Application.Dtos.Category;

public record UpdateCategoryRequest(
    string Name,
    CategoryImageDto? Image = null
);
