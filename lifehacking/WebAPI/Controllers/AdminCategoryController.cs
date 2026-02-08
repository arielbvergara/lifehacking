using Application.Dtos.Category;
using Application.Interfaces;
using Application.UseCases.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes administrative endpoints for managing categories.
///
/// All routes are scoped under <c>/api/admin/categories</c> and require the
/// <see cref="AuthorizationPoliciesConstants.AdminOnly"/> policy.
/// </summary>
[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]
[EnableRateLimiting(RateLimitingPolicies.Fixed)]
public class AdminCategoryController(
    CreateCategoryUseCase createCategoryUseCase,
    UpdateCategoryUseCase updateCategoryUseCase,
    DeleteCategoryUseCase deleteCategoryUseCase,
    ISecurityEventNotifier securityEventNotifier,
    ILogger<AdminCategoryController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Creates a new category with the specified name.
    /// Category names must be unique (case-insensitive) and between 2-100 characters.
    /// Soft-deleted categories are included in uniqueness checks to prevent name reuse.
    /// </remarks>
    /// <param name="request">The create category request containing the category name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created category with HTTP 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await createCategoryUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to create category: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.CategoryCreateFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["CategoryName"] = request.Name,
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var category = result.Value!;

        logger.LogInformation(
            "Admin {AdminId} created category {CategoryId} with name '{CategoryName}'",
            User.Identity?.Name,
            category.Id,
            category.Name);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.CategoryCreated,
            category.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["CategoryName"] = category.Name,
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(CreateCategory),
            new { id = category.Id },
            category);
    }

    /// <summary>
    /// Updates an existing category's name.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Updates the category with the specified ID.
    /// The new name must be unique (case-insensitive) and between 2-100 characters.
    /// Soft-deleted categories are included in uniqueness checks to prevent name reuse.
    /// Returns 404 if the category does not exist or is soft-deleted.
    /// </remarks>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="request">The update category request containing the new name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated category with HTTP 200 OK status.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await updateCategoryUseCase.ExecuteAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to update category {CategoryId}: {Message}", id, error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.CategoryUpdateFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["CategoryId"] = id.ToString(),
                    ["NewCategoryName"] = request.Name,
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var category = result.Value!;

        logger.LogInformation(
            "Admin {AdminId} updated category {CategoryId} to name '{CategoryName}'",
            User.Identity?.Name,
            category.Id,
            category.Name);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.CategoryUpdated,
            category.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["CategoryId"] = category.Id.ToString(),
                ["CategoryName"] = category.Name,
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return Ok(category);
    }

    /// <summary>
    /// Soft-deletes a category and all associated tips.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Soft-deletes the category with the specified ID
    /// and cascades the soft-delete to all tips associated with that category.
    /// Returns 404 if the category does not exist or is already soft-deleted.
    /// </remarks>
    /// <param name="id">The ID of the category to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP 204 No Content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await deleteCategoryUseCase.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to delete category {CategoryId}: {Message}", id, error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.CategoryDeleteFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["CategoryId"] = id.ToString(),
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        logger.LogInformation(
            "Admin {AdminId} deleted category {CategoryId}",
            User.Identity?.Name,
            id);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.CategoryDeleted,
            id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["CategoryId"] = id.ToString(),
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return NoContent();
    }
}
