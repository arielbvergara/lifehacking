using Application.Caching;
using Application.Interfaces;
using Domain.ValueObject;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;

/// <summary>
/// Service for invalidating cached data related to categories, tips, and dashboard statistics.
/// Centralizes cache invalidation logic to ensure consistency across the application.
/// </summary>
public sealed class CacheInvalidationService(IMemoryCache memoryCache) : ICacheInvalidationService
{
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <summary>
    /// Invalidates the cached category list.
    /// </summary>
    public void InvalidateCategoryList()
    {
        _memoryCache.Remove(CacheKeys.CategoryList);
    }

    /// <summary>
    /// Invalidates the cached admin dashboard.
    /// </summary>
    public void InvalidateDashboard()
    {
        _memoryCache.Remove(CacheKeys.AdminDashboard);
    }

    /// <summary>
    /// Invalidates the cache for a specific category by ID.
    /// </summary>
    /// <param name="categoryId">The category ID to invalidate.</param>
    public void InvalidateCategory(CategoryId categoryId)
    {
        ArgumentNullException.ThrowIfNull(categoryId);
        var cacheKey = CacheKeys.Category(categoryId);
        _memoryCache.Remove(cacheKey);
    }

    /// <summary>
    /// Invalidates both the category list and a specific category cache.
    /// </summary>
    /// <param name="categoryId">The category ID to invalidate.</param>
    public void InvalidateCategoryAndList(CategoryId categoryId)
    {
        InvalidateCategoryList();
        InvalidateCategory(categoryId);
    }
}
