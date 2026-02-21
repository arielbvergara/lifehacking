using Domain.ValueObject;

namespace Application.Caching;

/// <summary>
/// Centralized cache key definitions to ensure consistency across the application.
/// All cache keys used for category-related caching are defined here to prevent
/// key mismatches and drift between different parts of the codebase.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Cache key for the complete category list with tip counts.
    /// </summary>
    public const string CategoryList = "CategoryList";

    /// <summary>
    /// Prefix for individual category cache keys.
    /// </summary>
    private const string CategoryPrefix = "Category_";

    /// <summary>
    /// Builds a normalized cache key for a specific category.
    /// Uses the "D" format specifier to ensure consistent GUID formatting.
    /// </summary>
    /// <param name="categoryId">The category ID to build the cache key for.</param>
    /// <returns>The normalized cache key for the specified category.</returns>
    public static string Category(CategoryId categoryId)
    {
        ArgumentNullException.ThrowIfNull(categoryId);
        // Use "D" format for consistent GUID representation (lowercase with hyphens)
        return $"{CategoryPrefix}{categoryId.Value:D}";
    }

    /// <summary>
    /// Builds a normalized cache key for a specific category using a Guid.
    /// Uses the "D" format specifier to ensure consistent GUID formatting.
    /// </summary>
    /// <param name="categoryId">The category ID as a Guid.</param>
    /// <returns>The normalized cache key for the specified category.</returns>
    public static string Category(Guid categoryId)
    {
        // Use "D" format for consistent GUID representation (lowercase with hyphens)
        return $"{CategoryPrefix}{categoryId:D}";
    }
}
