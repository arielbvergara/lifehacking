namespace Application.Dtos.Dashboard;

/// <summary>
/// Response containing dashboard statistics grouped by entity type.
/// </summary>
public sealed record DashboardResponse
{
    /// <summary>
    /// Statistics for users.
    /// </summary>
    public required EntityStatistics Users { get; init; }

    /// <summary>
    /// Statistics for categories.
    /// </summary>
    public required EntityStatistics Categories { get; init; }

    /// <summary>
    /// Statistics for tips.
    /// </summary>
    public required EntityStatistics Tips { get; init; }
}
