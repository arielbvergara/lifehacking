namespace Application.Dtos.Tip;

public record TipDetailResponse(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<TipStepDto> Steps,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> Tags,
    string? VideoUrl,
    string? VideoUrlId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
