using Application.Dtos;
using Application.Dtos.Favorite;
using Application.Dtos.Tip;
using Application.Dtos.User;
using Application.Interfaces;
using Application.UseCases.Favorite;
using Application.UseCases.User;
using Domain.ValueObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes authenticated endpoints for managing user favorites.
///
/// All endpoints require authentication via JWT and operate on the current authenticated user's
/// favorites. Users can list their favorites with filtering/sorting/pagination, add tips to
/// their favorites, and remove tips from their favorites.
/// </summary>
[ApiController]
[Route("api/me/favorites")]
[Authorize]
public class FavoritesController(
    SearchUserFavoritesUseCase searchUserFavoritesUseCase,
    AddFavoriteUseCase addFavoriteUseCase,
    RemoveFavoriteUseCase removeFavoriteUseCase,
    MergeFavoritesUseCase mergeFavoritesUseCase,
    GetUserByExternalAuthIdUseCase getUserByExternalAuthIdUseCase,
    ILogger<FavoritesController> logger,
    ISecurityEventNotifier securityEventNotifier)
    : ControllerBase
{
    /// <summary>
    /// Retrieves the current authenticated user's list of favorite tips.
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of the user's favorites with full tip details. The user is
    /// resolved from the external authentication identifier (for example, the Firebase UID in
    /// the JWT <c>sub</c> claim). Supports filtering by category, searching by term, sorting,
    /// and pagination via query parameters.
    /// </remarks>
    /// <param name="q">Optional search term to filter favorites by tip title or description.</param>
    /// <param name="categoryId">Optional category ID to filter favorites by category.</param>
    /// <param name="tags">Optional list of tags to filter favorites.</param>
    /// <param name="orderBy">Sort field (Title, CreatedAt, UpdatedAt). Defaults to CreatedAt.</param>
    /// <param name="sortDirection">Sort direction (Ascending, Descending). Defaults to Descending.</param>
    /// <param name="pageNumber">Page number (minimum 1). Defaults to 1.</param>
    /// <param name="pageSize">Page size (1-100). Defaults to 10.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of favorites with tip details and pagination metadata.</returns>
    [HttpGet]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<PagedFavoritesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetMyFavorites(
        [FromQuery] string? q = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] List<string>? tags = null,
        [FromQuery] TipSortField? orderBy = null,
        [FromQuery] SortDirection? sortDirection = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

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
            Tags: tags?.AsReadOnly(),
            SortField: orderBy ?? TipSortField.CreatedAt,
            SortDirection: sortDirection ?? SortDirection.Descending,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var userId = UserId.Create(currentUser!.Id);
        var request = new SearchUserFavoritesRequest(userId, criteria);

        var result = await searchUserFavoritesUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to retrieve favorites for user {UserId}: {Message}",
                currentUser.Id, error.Message);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Adds a tip to the current authenticated user's favorites.
    /// </summary>
    /// <remarks>
    /// Adds the specified tip to the user's favorites collection. The user is resolved from the
    /// external authentication identifier (for example, the Firebase UID in the JWT <c>sub</c> claim).
    /// Returns 201 Created with the favorite details on success. Returns 409 Conflict if the tip
    /// is already in the user's favorites. Returns 404 Not Found if the tip does not exist.
    /// Emits security events for both success and failure cases for audit logging.
    /// </remarks>
    /// <param name="tipId">The ID of the tip to add to favorites.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created favorite with tip details.</returns>
    [HttpPost("{tipId}")]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType<FavoriteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddFavorite(
        [FromRoute] Guid tipId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        // Create value objects
        var userId = UserId.Create(currentUser!.Id);
        TipId tipIdValue;
        try
        {
            tipIdValue = TipId.Create(tipId);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid tip ID provided: {TipId}", tipId);
            return BadRequest(new { message = "Invalid tip ID format." });
        }

        var request = new AddFavoriteRequest(userId, tipIdValue);

        var result = await addFavoriteUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to add favorite for user {UserId}, tip {TipId}: {Message}",
                currentUser.Id, tipId, error.Message);

            // Emit failure security event
            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.FavoriteAddFailed,
                currentUser.Id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipId"] = tipId.ToString(),
                    ["ExceptionType"] = error.GetType().Name
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        // Emit success security event
        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.FavoriteAdded,
            currentUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TipId"] = tipId.ToString()
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetMyFavorites),
            new { },
            result.Value!);
    }

    /// <summary>
    /// Removes a tip from the current authenticated user's favorites.
    /// </summary>
    /// <remarks>
    /// Removes the specified tip from the user's favorites collection. The user is resolved from the
    /// external authentication identifier (for example, the Firebase UID in the JWT <c>sub</c> claim).
    /// Returns 204 No Content on success. Returns 404 Not Found if the tip is not in the user's favorites.
    /// Emits security events for both success and failure cases for audit logging.
    /// </remarks>
    /// <param name="tipId">The ID of the tip to remove from favorites.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{tipId}")]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RemoveFavorite(
        [FromRoute] Guid tipId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        // Create value objects
        var userId = UserId.Create(currentUser!.Id);
        TipId tipIdValue;
        try
        {
            tipIdValue = TipId.Create(tipId);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid tip ID provided: {TipId}", tipId);
            return BadRequest(new { message = "Invalid tip ID format." });
        }

        var request = new RemoveFavoriteRequest(userId, tipIdValue);

        var result = await removeFavoriteUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to remove favorite for user {UserId}, tip {TipId}: {Message}",
                currentUser.Id, tipId, error.Message);

            // Emit failure security event
            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.FavoriteRemoveFailed,
                currentUser.Id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipId"] = tipId.ToString(),
                    ["ExceptionType"] = error.GetType().Name
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        // Emit success security event
        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.FavoriteRemoved,
            currentUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TipId"] = tipId.ToString()
            },
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Merges client-stored anonymous favorites into the authenticated user's server-side favorites.
    /// </summary>
    /// <remarks>
    /// Accepts a list of tip IDs from client local storage and merges them into the user's
    /// server-side favorites. The operation performs validation, deduplication, and returns
    /// a detailed summary of the merge results including counts of added, skipped, and failed tips.
    /// 
    /// The merge operation is idempotent - calling it multiple times with the same tip IDs
    /// produces the same result. Existing favorites are never removed, only new ones are added.
    /// 
    /// Invalid or non-existent tip IDs are reported in the failed list but do not prevent
    /// valid tips from being added (partial success).
    /// </remarks>
    /// <param name="requestDto">The merge request containing a list of tip IDs from client local storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A merge summary with counts of added, skipped, and failed tips.</returns>
    [HttpPost("merge")]
    [EnableRateLimiting(RateLimitingPolicies.Strict)]
    [ProducesResponseType<MergeFavoritesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MergeFavorites(
        [FromBody] MergeFavoritesRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, errorResult) = await GetCurrentUserAsync(cancellationToken);
        if (errorResult is not null)
        {
            return errorResult;
        }

        // Validate request body
        if (requestDto?.TipIds is null)
        {
            logger.LogWarning("Merge favorites request body is null or missing tip IDs");
            return BadRequest(new { message = "Request body must contain a 'tipIds' array." });
        }

        // Convert GUIDs to TipId value objects
        var tipIds = new List<TipId>();
        var invalidGuids = new List<FailedTip>();

        foreach (var tipIdGuid in requestDto.TipIds)
        {
            try
            {
                tipIds.Add(TipId.Create(tipIdGuid));
            }
            catch (ArgumentException)
            {
                invalidGuids.Add(new FailedTip(tipIdGuid, "Invalid tip ID format"));
            }
        }

        // Create request
        var userId = UserId.Create(currentUser!.Id);
        var request = new MergeFavoritesRequest(userId, tipIds);

        var result = await mergeFavoritesUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to merge favorites for user {UserId}: {Message}",
                currentUser.Id, error.Message);

            // Emit failure security event
            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.FavoritesMergeFailed,
                currentUser.Id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["TipCount"] = requestDto.TipIds.Count.ToString(),
                    ["ExceptionType"] = error.GetType().Name
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        // Merge invalid GUIDs into the response
        var response = result.Value!;
        if (invalidGuids.Count > 0)
        {
            var allFailed = response.Failed.Concat(invalidGuids).ToList();
            response = response with { Failed = allFailed };
        }

        // Emit success security event
        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.FavoritesMerged,
            currentUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["TotalReceived"] = response.TotalReceived.ToString(),
                ["Added"] = response.Added.ToString(),
                ["Skipped"] = response.Skipped.ToString(),
                ["Failed"] = response.Failed.Count.ToString()
            },
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Resolves the current authenticated user from the external auth identifier.
    /// </summary>
    /// <remarks>
    /// This helper extracts the ExternalAuthId from JWT claims and resolves it to the internal
    /// user record. Returns appropriate error responses for missing authentication or non-existent users.
    /// </remarks>
    private async Task<(UserResponse? currentUser, IActionResult? errorResult)> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalAuthId = User.GetExternalAuthId();
        if (externalAuthId is null)
        {
            logger.LogWarning("Authenticated principal is missing external auth identifier claim.");
            return (null, Forbid());
        }

        var currentUserResult = await getUserByExternalAuthIdUseCase.ExecuteAsync(
            new GetUserByExternalAuthIdRequest(externalAuthId),
            cancellationToken);

        if (currentUserResult.IsFailure)
        {
            var error = currentUserResult.Error!;
            logger.LogError(error.InnerException, "Failed to resolve current user from external auth ID: {Message}",
                error.Message);

            IActionResult actionResult = error switch
            {
                Application.Exceptions.NotFoundException => NotFound(new { error.Message }),
                _ => Forbid()
            };

            return (null, actionResult);
        }

        return (currentUserResult.Value!, null);
    }
}
