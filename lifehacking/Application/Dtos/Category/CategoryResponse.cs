namespace Application.Dtos.Category;

public record CategoryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
