namespace Application.Dtos.Category;

public record CreateCategoryRequest(
    string Name,
    CategoryImageDto? Image = null
);
