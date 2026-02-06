using Application.Dtos.Tip;

namespace Application.Dtos.Favorite;

/// <summary>
/// Response containing a user's favorite with full tip details.
/// </summary>
/// <param name="TipId">The ID of the favorited tip.</param>
/// <param name="AddedAt">The timestamp when the tip was added to favorites.</param>
/// <param name="TipDetails">The complete details of the favorited tip.</param>
public record FavoriteResponse(
    Guid TipId,
    DateTime AddedAt,
    TipDetailResponse TipDetails);
