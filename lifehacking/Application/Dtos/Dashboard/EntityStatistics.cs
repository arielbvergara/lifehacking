namespace Application.Dtos.Dashboard;

/// <summary>
/// Represents statistics for a specific entity type (users, categories, or tips).
/// </summary>
public sealed record EntityStatistics
{
    /// <summary>
    /// Total count of all active entities.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Count of entities created in the previous calendar month.
    /// </summary>
    public int LastMonth { get; init; }

    /// <summary>
    /// Count of entities created in the current calendar month.
    /// </summary>
    public int ThisMonth { get; init; }
}
