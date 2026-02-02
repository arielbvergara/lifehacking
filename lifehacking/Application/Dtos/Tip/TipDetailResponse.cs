namespace Application.Dtos.Tip;

public record TipDetailResponse(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<TipStepDto> Steps,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> Tags,
    string? YouTubeUrl,
    string? YouTubeVideoId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
