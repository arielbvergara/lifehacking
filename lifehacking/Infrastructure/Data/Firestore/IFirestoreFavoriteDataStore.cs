using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;

namespace Infrastructure.Data.Firestore;

/// <summary>
/// Low-level Firestore data store interface for favorites operations.
/// Handles direct Firestore document manipulation.
/// </summary>
public interface IFirestoreFavoriteDataStore
{
    /// <summary>
    /// Retrieves a favorite by its composite key (userId_tipId).
    /// </summary>
    Task<UserFavorites?> GetByCompositeKeyAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new favorite to Firestore using composite document ID.
    /// </summary>
    Task<UserFavorites> AddAsync(UserFavorites favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a favorite from Firestore.
    /// </summary>
    Task<bool> RemoveAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches a user's favorites with filtering, sorting, and pagination.
    /// Returns tip IDs and total count.
    /// </summary>
    Task<(IReadOnlyList<TipId> tipIds, int totalCount)> SearchAsync(
        UserId userId,
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a favorite exists.
    /// </summary>
    Task<bool> ExistsAsync(UserId userId, TipId tipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the set of tip IDs that are already favorited by the user from the provided list.
    /// </summary>
    Task<IReadOnlySet<TipId>> GetExistingFavoritesAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple favorites in a batch operation.
    /// </summary>
    Task<IReadOnlyList<UserFavorites>> AddBatchAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default);
}
