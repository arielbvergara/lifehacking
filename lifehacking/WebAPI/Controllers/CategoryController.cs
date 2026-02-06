using Application.Dtos;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.UseCases.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    GetTipsByCategoryUseCase getTipsByCategoryUseCase,
    ILogger<CategoryController> logger) : ControllerBase
{
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
