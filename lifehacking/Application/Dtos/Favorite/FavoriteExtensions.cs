using Application.Dtos.Tip;
using DomainTip = Domain.Entities.Tip;

namespace Application.Dtos.Favorite;

/// <summary>
/// Extension methods for converting favorites to response DTOs.
/// </summary>
public static class FavoriteExtensions
{
    /// <summary>
    /// Converts a UserFavorites entity and its associated Tip to a FavoriteResponse DTO.
    /// </summary>
    /// <param name="favorite">The favorite entity.</param>
    /// <param name="tip">The associated tip entity.</param>
    /// <param name="categoryName">The name of the tip's category.</param>
    /// <returns>A FavoriteResponse containing the favorite metadata and full tip details.</returns>
    public static FavoriteResponse ToFavoriteResponse(
        this Domain.Entities.UserFavorites favorite,
        DomainTip tip,
        string categoryName)
    {
        var tipDetails = tip.ToTipDetailResponse(categoryName);

        return new FavoriteResponse(
            TipId: favorite.TipId.Value,
            AddedAt: favorite.AddedAt,
            TipDetails: tipDetails);
    }
}
