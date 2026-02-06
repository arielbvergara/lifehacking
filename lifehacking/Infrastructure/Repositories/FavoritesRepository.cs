using Application.Dtos;
using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for favorites using Firestore.
/// Coordinates between favorites data store and tips data store.
/// </summary>
public sealed class FavoritesRepository(
    IFirestoreFavoriteDataStore favoriteDataStore,
    IFirestoreTipDataStore tipDataStore) : IFavoritesRepository
{
    private readonly IFirestoreFavoriteDataStore _favoriteDataStore = favoriteDataStore ?? throw new ArgumentNullException(nameof(favoriteDataStore));
    private readonly IFirestoreTipDataStore _tipDataStore = tipDataStore ?? throw new ArgumentNullException(nameof(tipDataStore));

    public async Task<UserFavorites?> GetByUserAndTipAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        return await _favoriteDataStore.GetByCompositeKeyAsync(userId, tipId, cancellationToken);
    }

    public async Task<UserFavorites> AddAsync(
        UserFavorites favorite,
        CancellationToken cancellationToken = default)
    {
        return await _favoriteDataStore.AddAsync(favorite, cancellationToken);
    }

    public async Task<bool> RemoveAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        return await _favoriteDataStore.RemoveAsync(userId, tipId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Tip> tips, int totalCount)> SearchUserFavoritesAsync(
        UserId userId,
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Get paginated tip IDs from favorites
        var (tipIds, totalCount) = await _favoriteDataStore.SearchAsync(userId, criteria, cancellationToken);

        if (tipIds.Count == 0)
        {
            return (Array.Empty<Tip>(), 0);
        }

        // Fetch the actual tips
        var tips = new List<Tip>();
        foreach (var tipId in tipIds)
        {
            var tip = await _tipDataStore.GetByIdAsync(tipId, cancellationToken);
            if (tip is not null)
            {
                tips.Add(tip);
            }
        }

        // Apply additional filtering if specified in criteria
        IEnumerable<Tip> filtered = tips;

        // Filter by search term (title/description)
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.Trim();
            filtered = filtered.Where(tip =>
                tip.Title.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                tip.Description.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by category
        if (criteria.CategoryId.HasValue)
        {
            var categoryId = CategoryId.Create(criteria.CategoryId.Value);
            filtered = filtered.Where(tip => tip.CategoryId.Equals(categoryId));
        }

        // Filter by tags
        if (criteria.Tags is not null && criteria.Tags.Count > 0)
        {
            filtered = filtered.Where(tip =>
                criteria.Tags.Any(tag =>
                    tip.Tags.Any(tipTag =>
                        tipTag.Value.Equals(tag, StringComparison.OrdinalIgnoreCase))));
        }

        var filteredList = filtered.ToList();

        // Apply sorting based on tip properties
        var sorted = ApplySorting(filteredList, criteria);

        return (sorted, totalCount);
    }

    public async Task<bool> ExistsAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        return await _favoriteDataStore.ExistsAsync(userId, tipId, cancellationToken);
    }

    private static IReadOnlyList<Tip> ApplySorting(List<Tip> tips, TipQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (TipSortField.Title, SortDirection.Ascending) =>
                tips.OrderBy(t => t.Title.Value).ToList(),
            (TipSortField.Title, SortDirection.Descending) =>
                tips.OrderByDescending(t => t.Title.Value).ToList(),
            (TipSortField.CreatedAt, SortDirection.Ascending) =>
                tips.OrderBy(t => t.CreatedAt).ToList(),
            (TipSortField.CreatedAt, SortDirection.Descending) =>
                tips.OrderByDescending(t => t.CreatedAt).ToList(),
            (TipSortField.UpdatedAt, SortDirection.Ascending) =>
                tips.OrderBy(t => t.UpdatedAt ?? t.CreatedAt).ToList(),
            (TipSortField.UpdatedAt, SortDirection.Descending) =>
                tips.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ToList(),
            _ => tips // Default: maintain favorites order (by AddedAt from data store)
        };
    }
}
