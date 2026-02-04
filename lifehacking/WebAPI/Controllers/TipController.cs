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
public class TipController(GetTipByIdUseCase getTipByIdUseCase, ILogger<TipController> logger) : ControllerBase
{
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