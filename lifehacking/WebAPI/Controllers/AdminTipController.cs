using Application.Dtos.Tip;
using Application.Interfaces;
using Application.UseCases.Tip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes administrative endpoints for managing tips.
///
/// All routes are scoped under <c>/api/admin/tips</c> and require the
/// <see cref="AuthorizationPoliciesConstants.AdminOnly"/> policy.
/// </summary>
[ApiController]
[Route("api/admin/tips")]
[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]
[EnableRateLimiting(RateLimitingPolicies.Fixed)]
public class AdminTipController(
    CreateTipUseCase createTipUseCase,
    UpdateTipUseCase updateTipUseCase,
    DeleteTipUseCase deleteTipUseCase,
    ISecurityEventNotifier securityEventNotifier,
    ILogger<AdminTipController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new tip.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Creates a new tip with structured content
    /// including title, description, steps, category, tags, and optional video URL.
    /// Video URLs must match supported platforms (YouTube, Instagram, YouTube Shorts).
    /// The category must exist and not be soft-deleted.
    /// </remarks>
    /// <param name="request">The create tip request containing tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created tip with HTTP 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType<TipDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTip(
        [FromBody] CreateTipRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await createTipUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to create tip: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.TipCreateFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipTitle"] = request.Title,
                    ["CategoryId"] = request.CategoryId.ToString(),
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var tip = result.Value!;

        logger.LogInformation(
            "Admin {AdminId} created tip {TipId} with title '{TipTitle}'",
            User.Identity?.Name,
            tip.Id,
            tip.Title);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.TipCreated,
            tip.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TipTitle"] = tip.Title,
                ["CategoryId"] = tip.CategoryId.ToString(),
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(CreateTip),
            new { id = tip.Id },
            tip);
    }

    /// <summary>
    /// Updates an existing tip.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Updates the tip with the specified ID.
    /// All fields (title, description, steps, category, tags, video URL) can be updated.
    /// Video URLs must match supported platforms (YouTube, Instagram, YouTube Shorts).
    /// The category must exist and not be soft-deleted.
    /// Returns 404 if the tip does not exist.
    /// </remarks>
    /// <param name="id">The ID of the tip to update.</param>
    /// <param name="request">The update tip request containing updated tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated tip with HTTP 200 OK status.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<TipDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTip(
        [FromRoute] Guid id,
        [FromBody] UpdateTipRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await updateTipUseCase.ExecuteAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to update tip {TipId}: {Message}", id, error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.TipUpdateFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipId"] = id.ToString(),
                    ["NewTipTitle"] = request.Title,
                    ["CategoryId"] = request.CategoryId.ToString(),
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var tip = result.Value!;

        logger.LogInformation(
            "Admin {AdminId} updated tip {TipId} to title '{TipTitle}'",
            User.Identity?.Name,
            tip.Id,
            tip.Title);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.TipUpdated,
            tip.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TipId"] = tip.Id.ToString(),
                ["TipTitle"] = tip.Title,
                ["CategoryId"] = tip.CategoryId.ToString(),
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return Ok(tip);
    }

    /// <summary>
    /// Soft-deletes a tip.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Soft-deletes the tip with the specified ID.
    /// The tip is marked as deleted but not physically removed from storage.
    /// Returns 404 if the tip does not exist.
    /// </remarks>
    /// <param name="id">The ID of the tip to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP 204 No Content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTip(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await deleteTipUseCase.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to delete tip {TipId}: {Message}", id, error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.TipDeleteFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipId"] = id.ToString(),
                    ["ExceptionType"] = error.Type.ToString()
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        logger.LogInformation(
            "Admin {AdminId} deleted tip {TipId}",
            User.Identity?.Name,
            id);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.TipDeleted,
            id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TipId"] = id.ToString(),
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return NoContent();
    }
}
