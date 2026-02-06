using Domain.ValueObject;

namespace Application.Dtos.Favorite;

/// <summary>
/// Request to add a tip to a user's favorites.
/// </summary>
/// <param name="UserId">The ID of the user adding the favorite.</param>
/// <param name="TipId">The ID of the tip to favorite.</param>
public record AddFavoriteRequest(UserId UserId, TipId TipId);
