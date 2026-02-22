namespace Application.Dtos.Category;

public record CreateCategoryRequest(
    string Name,
    ImageDto? Image = null
);
