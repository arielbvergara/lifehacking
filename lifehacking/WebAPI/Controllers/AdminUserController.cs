using Application.Dtos;
using Application.Dtos.User;
using Application.Interfaces;
using Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Authorization;
using WebAPI.DTOs;
using WebAPI.ErrorHandling;
using WebAPI.RateLimiting;

namespace WebAPI.Controllers;

/// <summary>
/// Exposes administrative endpoints for managing users.
///
/// All routes are scoped under <c>/api/admin/User</c> and require the
/// <see cref="AuthorizationPoliciesConstants.AdminOnly"/> policy.
/// </summary>
[ApiController]
[Route(AdminUserRoutePrefix)]
[Authorize(Policy = AuthorizationPoliciesConstants.AdminOnly)]
public class AdminUserController(
    GetUsersUseCase getUsersUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    GetUserByEmailUseCase getUserByEmailUseCase,
    UpdateUserNameUseCase updateUserNameUseCase,
    DeleteUserUseCase deleteUserUseCase,
    CreateAdminUserUseCase createAdminUserUseCase,
    ILogger<AdminUserController> logger,
    ISecurityEventNotifier securityEventNotifier)
    : ControllerBase
{
    private const string AdminUserRoutePrefix = "api/admin/User";

    /// <summary>
    /// Creates a new admin user with Firebase authentication credentials.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Creates a new user with admin privileges
    /// using the provided email, display name, and password.
    ///
    /// **Validation Rules:**
    /// - **Email**: Required, must be a valid email address
    /// - **DisplayName**: Required
    /// - **Password**: Required, minimum 12 characters
    /// </remarks>
    /// <param name="dto">The admin user creation request containing email, display name, and password.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created admin user with HTTP 200 OK status.</returns>
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminUserDto dto, CancellationToken cancellationToken)
    {
        var result = await createAdminUserUseCase.ExecuteAsync(
            new CreateAdminUserRequest(dto.Email, dto.DisplayName, dto.Password),
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to create admin user: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserCreateFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var adminUser = result.Value!;

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserCreated,
            adminUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return Ok(adminUser);
    }

    /// <summary>
    /// Gets a paginated list of users. Restricted to administrators.
    /// </summary>
    /// <remarks>
    /// Supports searching across email, name, and id, as well as ordering by email, name,
    /// and creation timestamp in both ascending and descending directions.
    /// </remarks>
    /// <param name="search">Optional search term to filter users by email, name, or id.</param>
    /// <param name="orderBy">Sort field (Email, Name, CreatedAt). Defaults to CreatedAt.</param>
    /// <param name="sortDirection">Sort direction (Ascending, Descending). Defaults to Descending.</param>
    /// <param name="pageNumber">Page number for pagination (starts at 1).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="isDeleted">Optional filter for deleted status.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A paginated list of users with pagination metadata.</returns>
    [HttpGet]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<PagedUsersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserSortField? orderBy,
        [FromQuery] SortDirection? sortDirection,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserContext = User.ToCurrentUserContext();

        var request = new GetUsersRequest(
            search,
            orderBy ?? UserSortField.CreatedAt,
            sortDirection ?? SortDirection.Descending,
            pageNumber,
            pageSize,
            isDeleted,
            currentUserContext);

        var result = await getUsersUseCase.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to get list of users. Error: {error}", error.Message);
            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Gets a user by internal identifier.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators.
    /// </remarks>
    /// <param name="id">The internal GUID identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user details.</returns>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserContext = User.ToCurrentUserContext();
        var result = await getUserByIdUseCase.ExecuteAsync(new GetUserByIdRequest(id, currentUserContext), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to get user: {Message}", error.Message);
            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators.
    /// </remarks>
    /// <param name="email">The email address of the user to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user details.</returns>
    [HttpGet("email/{email}")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var currentUserContext = User.ToCurrentUserContext();
        var result = await getUserByEmailUseCase.ExecuteAsync(new GetUserByEmailRequest(email, currentUserContext), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to get user by email: {Message}", error.Message);
            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        return Ok(result.Value!);
    }

    /// <summary>
    /// Updates the display name of a user identified by id.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators.
    /// </remarks>
    /// <param name="id">The internal GUID identifier of the user to update.</param>
    /// <param name="dto">The request containing the new display name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated user details.</returns>
    [HttpPut("{id:guid}/name")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiValidationErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateUserNameById(Guid id, [FromBody] UpdateUserNameDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserContext = User.ToCurrentUserContext();
        var result = await updateUserNameUseCase.ExecuteAsync(
            new UpdateUserNameRequest(id, dto.NewName, currentUserContext),
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to update user name: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserUpdateFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var updatedUser = result.Value!;

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserUpdated,
            updatedUser.Id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return Ok(updatedUser);
    }

    /// <summary>
    /// Deletes a user identified by id.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Soft-deletes the user with the specified ID.
    /// </remarks>
    /// <param name="id">The internal GUID identifier of the user to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP 204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitingPolicies.Fixed)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteUserById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserContext = User.ToCurrentUserContext();
        var result = await deleteUserUseCase.ExecuteAsync(new DeleteUserRequest(id, currentUserContext), cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to delete user: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.UserDeleteFailed,
                id.ToString(),
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    { "Route", HttpContext.Request.Path },
                    { "ExceptionType", error.Type.ToString() }
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.UserDeleted,
            id.ToString(),
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                { "Route", HttpContext.Request.Path }
            },
            cancellationToken);

        return NoContent();
    }
}
