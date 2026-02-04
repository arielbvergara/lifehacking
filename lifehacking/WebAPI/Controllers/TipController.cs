using Application.Dtos;
using Application.Dtos.Tip;
using Application.UseCases.Tip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes public tip endpoints for browsing and retrieving tip content.
/// 
/// These endpoints are publicly accessible and do not require authentication,
/// allowing anonymous users to browse and view tip details.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TipController(
    GetTipByIdUseCase getTipByIdUseCase,
    SearchTipsUseCase searchTipsUseCase,
    ILogger<TipController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for tips with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="q">Search term to match against tip title and description.</param>
    /// <param name="categoryId">Filter tips by category ID.</param>
    /// <param name="tags">Filter tips by tags (can specify multiple).</param>
    /// <param name="orderBy">Field to sort by (CreatedAt, UpdatedAt, Title).</param>
    /// <param name="sortDirection">Sort direction (Ascending or Descending).</param>
    /// <param name="pageNumber">Page number for pagination (starts at 1).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns a paginated list of tip summaries matching the search criteria,
    /// along with pagination metadata including total count and page information.
    /// </returns>
    /// <remarks>
    /// This endpoint is publicly accessible and does not require authentication.
    /// It supports comprehensive search and filtering capabilities for browsing tips.
    /// </remarks>
    [HttpGet]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<PagedTipsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchTips(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] string[]? tags,
        [FromQuery] TipSortField? orderBy,
        [FromQuery] SortDirection? sortDirection,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            logger.LogWarning("Invalid page number provided: {PageNumber}. Must be >= 1", pageNumber);
            return BadRequest(new { message = "Page number must be greater than or equal to 1." });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            logger.LogWarning("Invalid page size provided: {PageSize}. Must be between 1 and 100", pageSize);
            return BadRequest(new { message = "Page size must be between 1 and 100." });
        }

        // Create query criteria
        var criteria = new TipQueryCriteria(
            SearchTerm: q,
            CategoryId: categoryId,
            Tags: tags?.ToList(),
            SortField: orderBy ?? TipSortField.CreatedAt,
            SortDirection: sortDirection ?? SortDirection.Descending,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var request = new SearchTipsRequest(criteria);

        var result = await searchTipsUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to search tips: {Message}", error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var searchResults = result.Value!;
        logger.LogInformation("Successfully searched tips. Found {TotalItems} results, returning page {PageNumber} of {TotalPages}",
            searchResults.Metadata.TotalItems, searchResults.Metadata.PageNumber, searchResults.Metadata.TotalPages);

        return Ok(searchResults);
    }

    /// <summary>
    /// Retrieves the full details of a specific tip by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the tip to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns the complete tip details including title, description, ordered steps, 
    /// category information, tags, and optional YouTube link.
    /// </returns>
    /// <remarks>
    /// This endpoint is publicly accessible and does not require authentication.
    /// It returns comprehensive tip information suitable for display to end users.
    /// </remarks>
    [HttpGet("{id}")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<TipDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTipById(string id, CancellationToken cancellationToken)
    {
        // Validate that the provided ID is a valid GUID
        if (!Guid.TryParse(id, out var tipId))
        {
            logger.LogWarning("Invalid tip ID format provided: '{TipId}'", id);
            return BadRequest(new { message = $"Invalid tip ID format: '{id}'. Expected a valid GUID." });
        }

        var request = new GetTipByIdRequest(tipId);

        var result = await getTipByIdUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to retrieve tip with ID '{TipId}': {Message}", tipId, error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var tipDetail = result.Value!;
        logger.LogInformation("Successfully retrieved tip with ID '{TipId}' - Title: '{Title}'", tipId, tipDetail.Title);

        return Ok(tipDetail);
    }
}
