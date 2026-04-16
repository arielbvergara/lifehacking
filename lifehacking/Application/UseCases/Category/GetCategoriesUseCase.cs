using Application.Caching;
using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for retrieving all non-deleted categories with tip counts.
/// </summary>
public class GetCategoriesUseCase(
    ICategoryRepository categoryRepository,
    ITipRepository tipRepository,
    IMemoryCache memoryCache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly ICategoryRepository _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    private readonly ITipRepository _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <summary>
    /// Executes the use case to retrieve all categories with tip counts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the category list response or an application exception.</returns>
    public async Task<Result<CategoryListResponse, AppException>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_memoryCache.TryGetValue(CacheKeys.CategoryList, out CategoryListResponse? cachedResponse) && cachedResponse is not null)
        {
            return Result<CategoryListResponse, AppException>.Ok(cachedResponse);
        }

        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);

            // Execute tip count queries sequentially — EF Core's DbContext is not thread-safe,
            // so concurrent queries on the same scoped instance would throw. The result is cached
            // for 1 hour so the N-query cost is amortized.
            var categoryResponses = new List<CategoryResponse>(categories.Count);
            foreach (var category in categories)
            {
                var tipCount = await _tipRepository.CountByCategoryAsync(category.Id, cancellationToken);
                categoryResponses.Add(category.ToCategoryResponse(tipCount));
            }

            var response = new CategoryListResponse(categoryResponses.ToList());

            // Cache the response
            _memoryCache.Set(CacheKeys.CategoryList, response, CacheDuration);

            return Result<CategoryListResponse, AppException>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<CategoryListResponse, AppException>.Fail(
                new InfraException("Failed to retrieve categories", ex));
        }
    }
}
