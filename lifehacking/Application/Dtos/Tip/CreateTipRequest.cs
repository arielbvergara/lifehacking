namespace Application.Dtos.Tip;

public record CreateTipRequest(
    string Title,
    string Description,
    IReadOnlyList<TipStepRequest> Steps,
    Guid CategoryId,
    IReadOnlyList<string>? Tags,
    string? VideoUrl,
    ImageDto? Image = null
);
