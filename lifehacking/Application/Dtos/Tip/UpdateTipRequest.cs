namespace Application.Dtos.Tip;

public record UpdateTipRequest(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<TipStepRequest> Steps,
    Guid CategoryId,
    IReadOnlyList<string>? Tags,
    string? VideoUrl,
    TipImageDto? Image = null
);
