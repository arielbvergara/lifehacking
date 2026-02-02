using Application.Dtos.User;

namespace Application.Dtos.Tip;

public sealed record TipQueryCriteria(
    string? SearchTerm,
    Guid? CategoryId,
    IReadOnlyList<string>? Tags,
    TipSortField SortField,
    SortDirection SortDirection,
    int PageNumber,
    int PageSize
);
