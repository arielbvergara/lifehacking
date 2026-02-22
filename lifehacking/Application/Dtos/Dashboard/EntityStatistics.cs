namespace Application.Dtos.Dashboard;

/// <summary>
/// Represents statistics for a specific entity type (users, categories, or tips).
/// </summary>
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
    /// Count of entities created yesterday (previous calendar day).
    /// </summary>
    public int LastDay { get; init; }

    /// <summary>
    /// Count of entities created today (current calendar day).
    /// </summary>
    public int ThisDay { get; init; }

    /// <summary>
    /// Count of entities created in the previous calendar week (Monday-Sunday).
    /// </summary>
    public int LastWeek { get; init; }

    /// <summary>
    /// Count of entities created in the current calendar week (Monday to now).
    /// </summary>
    public int ThisWeek { get; init; }

    /// <summary>
    /// Count of entities created in the previous calendar month.
    /// </summary>
    public int LastMonth { get; init; }

    /// <summary>
    /// Count of entities created in the current calendar month.
    /// </summary>
    public int ThisMonth { get; init; }

    /// <summary>
    /// Count of entities created in the previous calendar year.
    /// </summary>
    public int LastYear { get; init; }

    /// <summary>
    /// Count of entities created in the current calendar year.
    /// </summary>
    public int ThisYear { get; init; }
}
