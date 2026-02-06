using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;

namespace Application.Interfaces;

/// <summary>
/// Repository contract for managing user favorites.
/// Provides operations to add, remove, and query favorite tips for users.
/// </summary>
public interface IFavoritesRepository
{
    /// <summary>
    /// Retrieves a specific favorite by user ID and tip ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="tipId">The ID of the tip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The favorite if found, otherwise null.</returns>
    Task<UserFavorites?> GetByUserAndTipAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new favorite to the repository.
    /// </summary>
    /// <param name="favorite">The favorite to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added favorite.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the favorite already exists.</exception>
    Task<UserFavorites> AddAsync(UserFavorites favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a favorite from the repository.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="tipId">The ID of the tip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the favorite was removed, false if it didn't exist.</returns>
    Task<bool> RemoveAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches a user's favorites with filtering, sorting, and pagination.
    /// Returns the full tip details for each favorite.
    /// </summary>
    /// <param name="userId">The ID of the user whose favorites to search.</param>
    /// <param name="criteria">The query criteria including filters, sort, and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the list of tips and the total count of matching favorites.</returns>
    Task<(IReadOnlyList<Tip> tips, int totalCount)> SearchUserFavoritesAsync(
        UserId userId,
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific tip is in a user's favorites.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="tipId">The ID of the tip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tip is favorited, otherwise false.</returns>
    Task<bool> ExistsAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);
}
