using Application.Dtos.User;

namespace Application.Dtos.Favorite;

/// <summary>
/// Paginated response containing a user's favorites with metadata.
/// </summary>
/// <param name="Favorites">The list of favorites for the current page.</param>
/// <param name="Metadata">Pagination metadata including total count and page information.</param>
public record PagedFavoritesResponse(
    IReadOnlyList<FavoriteResponse> Favorites,
    PaginationMetadata Metadata);
