using Application.Dtos.Tip;
using Domain.ValueObject;

namespace Application.Dtos.Favorite;

/// <summary>
/// Request to search a user's favorites with filtering, sorting, and pagination.
/// </summary>
/// <param name="UserId">The ID of the user whose favorites to search.</param>
/// <param name="Criteria">The query criteria including filters, sort, and pagination.</param>
public record SearchUserFavoritesRequest(UserId UserId, TipQueryCriteria Criteria);
