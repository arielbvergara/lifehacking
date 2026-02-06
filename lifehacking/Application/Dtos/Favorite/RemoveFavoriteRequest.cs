using Domain.ValueObject;

namespace Application.Dtos.Favorite;

/// <summary>
/// Request to remove a tip from a user's favorites.
/// </summary>
/// <param name="UserId">The ID of the user removing the favorite.</param>
/// <param name="TipId">The ID of the tip to remove from favorites.</param>
public record RemoveFavoriteRequest(UserId UserId, TipId TipId);
