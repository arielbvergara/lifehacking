using Application.Dtos;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.UseCases.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes public category endpoints for browsing categories and category-based tip discovery.
/// 
/// These endpoints are publicly accessible and do not require authentication,
/// allowing anonymous users to discover categories and browse tips by category.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class CategoryController(
    GetCategoriesUseCase getCategoriesUseCase,
    GetCategoryByIdUseCase getCategoryByIdUseCase,
    GetTipsByCategoryUseCase getTipsByCategoryUseCase,
    IMemoryCache memoryCache,
    ILogger<CategoryController> logger) : ControllerBase
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
    /// <summary>
    /// Retrieves all available categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns a list of all non-deleted categories available in the system.
    /// </returns>
    /// <remarks>
    /// This endpoint is publicly accessible and does not require authentication.
    /// It returns all categories that users can browse to discover tips.
    /// </remarks>
    [HttpGet]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<CategoryListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving all categories");

        var result = await getCategoriesUseCase.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to retrieve categories: {Message}", error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var categories = result.Value!;
        logger.LogInformation("Successfully retrieved {Count} categories", categories.Items.Count);

        return Ok(categories);
    }

    /// <summary>
    /// Retrieves a specific category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns the category details including name, timestamps, and image metadata.
    /// </returns>
    /// <remarks>
    /// This endpoint is publicly accessible and does not require authentication.
    /// Returns 404 Not Found if the category does not exist or has been deleted.
    /// Returns 400 Bad Request if the provided ID is not a valid GUID format.
    /// </remarks>
    [HttpGet("{id}")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoryById(
        string id,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving category with ID '{CategoryId}'", id);

        // Validate GUID format
        if (!Guid.TryParse(id, out var categoryGuid))
        {
            logger.LogWarning("Invalid category ID format provided: '{CategoryId}'", id);
            
            var errorResponse = new ApiErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Type = ErrorResponseTypes.ValidationErrorType,
                Title = ErrorResponseTitles.ValidationErrorTitle,
                Detail = $"Invalid category ID format: '{id}'. Expected a valid GUID.",
                Instance = HttpContext.Request.Path.Value,
                CorrelationId = HttpContext.TraceIdentifier
            };
            
            return BadRequest(errorResponse);
        }

        // Check cache first
        var cacheKey = $"Category_{id}";
        if (memoryCache.TryGetValue(cacheKey, out CategoryResponse? cachedResponse) && cachedResponse is not null)
        {
            logger.LogInformation("Returning cached category with ID '{CategoryId}'", id);
            return Ok(cachedResponse);
        }

        // Call use case
        var result = await getCategoryByIdUseCase.ExecuteAsync(categoryGuid, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to retrieve category with ID '{CategoryId}': {Message}", id, error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var category = result.Value!;
        logger.LogInformation("Successfully retrieved category with ID '{CategoryId}' - Name: '{Name}', TipCount: {TipCount}", id, category.Name, category.TipCount);

        // Cache the response
        memoryCache.Set(cacheKey, category, _cacheDuration);

        return Ok(category);
    }

    /// <summary>
    /// Retrieves tips belonging to a specific category with pagination and sorting support.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="orderBy">Field to sort by (CreatedAt, UpdatedAt, Title).</param>
    /// <param name="sortDirection">Sort direction (Ascending or Descending).</param>
    /// <param name="pageNumber">Page number for pagination (starts at 1).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns a paginated list of tip summaries belonging to the specified category,
    /// along with pagination metadata including total count and page information.
    /// </returns>
    /// <remarks>
    /// This endpoint is publicly accessible and does not require authentication.
    /// It supports pagination and sorting for browsing tips within a specific category.
    /// Returns 404 Not Found if the category does not exist.
    /// Returns 200 OK with an empty array if the category exists but has no tips.
    /// </remarks>
    [HttpGet("{id}/tips")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<PagedTipsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTipsByCategory(
        string id,
        [FromQuery] TipSortField? orderBy,
        [FromQuery] SortDirection? sortDirection,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving tips for category {CategoryId}", id);

        var request = new GetTipsByCategoryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            SortDirection = sortDirection
        };

        var result = await getTipsByCategoryUseCase.ExecuteAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to retrieve tips for category '{CategoryId}': {Message}", id, error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var tips = result.Value!;
        logger.LogInformation("Successfully retrieved {Count} tips for category '{CategoryId}' (page {PageNumber} of {TotalPages})",
            tips.Items.Count, id, tips.Metadata.PageNumber, tips.Metadata.TotalPages);

        return Ok(tips);
    }
}
