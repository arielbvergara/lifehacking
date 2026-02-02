using Application.Dtos.User;

namespace Application.Dtos.Tip;

public record PagedTipsResponse(
    IReadOnlyList<TipSummaryResponse> Items,
    PaginationMetadata Metadata
);
