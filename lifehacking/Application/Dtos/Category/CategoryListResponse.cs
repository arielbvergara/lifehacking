namespace Application.Dtos.Category;

public record CategoryListResponse(
    IReadOnlyList<CategoryResponse> Items
);
