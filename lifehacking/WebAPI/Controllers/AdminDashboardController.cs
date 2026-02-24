using Application.Dtos.Dashboard;
using Application.UseCases.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.ErrorHandling;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes administrative endpoints for retrieving dashboard statistics.
///
/// All routes are scoped under <c>/api/admin/dashboard</c> and require the AdminOnly policy.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDashboardController(
    GetDashboardUseCase getDashboardUseCase,
    ILogger<AdminDashboardController> logger) : ControllerBase
{
    private readonly GetDashboardUseCase _getDashboardUseCase = getDashboardUseCase ?? throw new ArgumentNullException(nameof(getDashboardUseCase));

    /// <summary>
    /// Retrieves dashboard statistics including user, category, and tip counts.
    /// Results are cached for 1 hour.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard statistics grouped by entity type.</returns>
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var request = new GetDashboardRequest();
        var result = await _getDashboardUseCase.ExecuteAsync(request, cancellationToken);

        return result.Match(
            success => Ok(success),
            failure =>
            {
                logger.LogError(failure.InnerException, "Failed to retrieve dashboard: {Message}", failure.Message);
                return this.ToActionResult(failure, HttpContext.TraceIdentifier);
            });
    }
}
