using Domain.ValueObject;

namespace Application.Interfaces;

/// <summary>
/// Service for invalidating cached data related to categories, tips, and dashboard statistics.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates the cached category list.
    /// </summary>
    void InvalidateCategoryList();

    /// <summary>
    /// Invalidates the cache for a specific category by ID.
    /// </summary>
    /// <param name="categoryId">The category ID to invalidate.</param>
    void InvalidateCategory(CategoryId categoryId);

    /// <summary>
    /// Invalidates both the category list and a specific category cache.
    /// </summary>
    /// <param name="categoryId">The category ID to invalidate.</param>
    void InvalidateCategoryAndList(CategoryId categoryId);

    /// <summary>
    /// Invalidates the cached admin dashboard.
    /// </summary>
    void InvalidateDashboard();
}
