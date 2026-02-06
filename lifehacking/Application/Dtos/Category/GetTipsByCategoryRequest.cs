namespace Application.Dtos.Category;

using Tip;

/// <summary>
/// Request for retrieving tips belonging to a specific category with pagination and sorting support.
/// </summary>
public sealed record GetTipsByCategoryRequest
{
    /// <summary>
    /// The page number to retrieve (1-based). Default is 1.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// The number of items per page. Default is 10. Maximum is 100.
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    /// The field to sort by. Default is CreatedAt.
    /// </summary>
    public TipSortField? OrderBy { get; init; }

    /// <summary>
    /// The sort direction. Default is Descending.
    /// </summary>
    public SortDirection? SortDirection { get; init; }
}
