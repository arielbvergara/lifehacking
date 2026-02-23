using Application.Dtos.Dashboard;
using Application.UseCases.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebAPI.ErrorHandling;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDashboardController(
    GetDashboardUseCase getDashboardUseCase,
    IMemoryCache memoryCache,
    ILogger<AdminDashboardController> logger) : ControllerBase
{
    private const string CacheKey = "AdminDashboard";
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);

    private readonly GetDashboardUseCase _getDashboardUseCase = getDashboardUseCase ?? throw new ArgumentNullException(nameof(getDashboardUseCase));
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <summary>
    /// Retrieves dashboard statistics including user, category, and tip counts.
    /// Results are cached for 24 hours.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard statistics grouped by entity type.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out DashboardResponse? cachedResponse) && cachedResponse is not null)
        {
            return Ok(cachedResponse);
        }

        var request = new GetDashboardRequest();
        var result = await _getDashboardUseCase.ExecuteAsync(request, cancellationToken);

        return result.Match(
            success =>
            {
                _memoryCache.Set(CacheKey, success, _cacheDuration);
                return Ok(success);
            },
            failure =>
            {
                logger.LogError(failure.InnerException, "Failed to retrieve dashboard: {Message}", failure.Message);
                return this.ToActionResult(failure, HttpContext.TraceIdentifier);
            });
    }
}
