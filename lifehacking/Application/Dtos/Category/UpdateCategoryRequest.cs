namespace Application.Dtos.Category;

public record UpdateCategoryRequest(
    Guid Id,
    string Name
);
