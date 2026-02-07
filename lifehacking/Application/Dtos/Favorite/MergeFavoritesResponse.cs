namespace Application.Dtos.Favorite;

/// <summary>
/// Response containing the results of a favorites merge operation.
/// </summary>
/// <param name="TotalReceived">The total number of tip IDs received in the merge request.</param>
/// <param name="Added">The number of favorites successfully added.</param>
/// <param name="Skipped">The number of tips that were already in the user's favorites.</param>
/// <param name="Failed">The list of tip IDs that failed validation with error details.</param>
public record MergeFavoritesResponse(
    int TotalReceived,
    int Added,
    int Skipped,
    IReadOnlyList<FailedTip> Failed
);

/// <summary>
/// Represents a tip ID that failed validation during the merge operation.
/// </summary>
/// <param name="TipId">The tip ID that failed.</param>
/// <param name="ErrorMessage">A description of why the tip failed validation.</param>
public record FailedTip(
    Guid TipId,
    string ErrorMessage
);
