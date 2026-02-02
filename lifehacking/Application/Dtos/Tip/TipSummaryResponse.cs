namespace Application.Dtos.Tip;

public record TipSummaryResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> Tags,
    string? YouTubeUrl,
    DateTime CreatedAt
);
