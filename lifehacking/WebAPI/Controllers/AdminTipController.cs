using Application.Dtos;
using Application.Dtos.Tip;
using Application.Interfaces;
using Application.UseCases;
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
    UploadImageUseCase uploadImageUseCase,
    ISecurityEventNotifier securityEventNotifier,
    ILogger<AdminTipController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new tip.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Creates a new tip with structured content
    /// including title, description, steps, category, tags, optional video URL, and optional image.
    /// </remarks>
    /// <param name="request">The create tip request containing tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created tip with HTTP 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType<TipDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                ["HasImage"] = (tip.Image != null).ToString(),
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
    ///
    /// **Validation Rules:**
    /// - **Title**: Required, 5-200 characters (trimmed)
    /// - **Description**: Required, 10-2000 characters (trimmed)
    /// - **Steps**: Required, at least one step
    ///   - Step Number: Must be >= 1
    ///   - Step Description: Required, 10-500 characters (trimmed)
    /// - **CategoryId**: Required, must exist and not be soft-deleted
    /// - **Tags**: Optional, maximum 10 tags
    ///   - Each tag: 1-50 characters (trimmed)
    /// - **VideoUrl**: Optional, must be valid URL from supported platforms
    ///   - YouTube Watch: `https://www.youtube.com/watch?v=*`
    ///   - YouTube Shorts: `https://www.youtube.com/shorts/*`
    ///   - Instagram: `https://www.instagram.com/p/*`
    ///
    /// **Possible Error Responses:**
    ///
    /// **400 Bad Request** - Validation error with field-level details:
    /// ```json
    /// {
    ///   "status": 400,
    ///   "type": "https://httpstatuses.io/400/validation-error",
    ///   "title": "Validation error",
    ///   "detail": "One or more validation errors occurred.",
    ///   "instance": "/api/admin/tips/{id}",
    ///   "correlationId": "abc123",
    ///   "errors": {
    ///     "Description": ["Tip description must be at least 10 characters"]
    ///   }
    /// }
    /// ```
    ///
    /// **404 Not Found** - Tip does not exist:
    /// ```json
    /// {
    ///   "status": 404,
    ///   "type": "https://httpstatuses.io/404/resource-not-found",
    ///   "title": "Resource not found",
    ///   "detail": "Tip with id '123e4567-e89b-12d3-a456-426614174000' was not found.",
    ///   "instance": "/api/admin/tips/123e4567-e89b-12d3-a456-426614174000",
    ///   "correlationId": "def456"
    /// }
    /// ```
    ///
    /// **404 Not Found** - Category does not exist or is soft-deleted:
    /// ```json
    /// {
    ///   "status": 404,
    ///   "type": "https://httpstatuses.io/404/resource-not-found",
    ///   "title": "Resource not found",
    ///   "detail": "Category with id '123e4567-e89b-12d3-a456-426614174000' was not found.",
    ///   "instance": "/api/admin/tips/{id}",
    ///   "correlationId": "ghi789"
    /// }
    /// ```
    ///
    /// **500 Internal Server Error** - Unexpected server error:
    /// ```json
    /// {
    ///   "status": 500,
    ///   "type": "https://httpstatuses.io/500/infrastructure-error",
    ///   "title": "Infrastructure error",
    ///   "detail": "An unexpected error occurred while processing your request.",
    ///   "instance": "/api/admin/tips/{id}",
    ///   "correlationId": "jkl012"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
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
    ///
    /// **Possible Error Responses:**
    ///
    /// **404 Not Found** - Tip does not exist or is already soft-deleted:
    /// ```json
    /// {
    ///   "status": 404,
    ///   "type": "https://httpstatuses.io/404/resource-not-found",
    ///   "title": "Resource not found",
    ///   "detail": "Tip with id '123e4567-e89b-12d3-a456-426614174000' was not found.",
    ///   "instance": "/api/admin/tips/123e4567-e89b-12d3-a456-426614174000",
    ///   "correlationId": "abc123"
    /// }
    /// ```
    ///
    /// **500 Internal Server Error** - Unexpected server error:
    /// ```json
    /// {
    ///   "status": 500,
    ///   "type": "https://httpstatuses.io/500/infrastructure-error",
    ///   "title": "Infrastructure error",
    ///   "detail": "An unexpected error occurred while processing your request.",
    ///   "instance": "/api/admin/tips/{id}",
    ///   "correlationId": "def456"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
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

    /// <summary>
    /// Uploads a tip image to AWS S3 storage.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Uploads an image file to S3 with validation
    /// for size, content type, and format. Returns complete image metadata including CloudFront CDN URL.
    ///
    /// **Validation Rules:**
    /// - **File**: Required, maximum 5MB
    /// - **Content Type**: Must be image/jpeg, image/png, image/gif, or image/webp
    /// - **Format**: Magic bytes must match declared content type
    ///
    /// **Possible Error Responses:**
    ///
    /// **400 Bad Request** - Validation error with field-level details:
    /// ```json
    /// {
    ///   "status": 400,
    ///   "type": "https://httpstatuses.io/400/validation-error",
    ///   "title": "Validation error",
    ///   "detail": "One or more validation errors occurred.",
    ///   "instance": "/api/admin/tips/images",
    ///   "correlationId": "abc123",
    ///   "errors": {
    ///     "File": ["File size cannot exceed 5MB"]
    ///   }
    /// }
    /// ```
    ///
    /// **500 Internal Server Error** - Unexpected server error:
    /// ```json
    /// {
    ///   "status": 500,
    ///   "type": "https://httpstatuses.io/500/infrastructure-error",
    ///   "title": "Infrastructure error",
    ///   "detail": "An unexpected error occurred while processing your request.",
    ///   "instance": "/api/admin/tips/images",
    ///   "correlationId": "def456"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
    /// </remarks>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The uploaded image metadata with HTTP 201 Created status.</returns>
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadTipImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file is provided
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Image upload failed: No file provided");

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.TipImageUploadFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["Reason"] = "NoFileProvided",
                    ["AdminId"] = User.Identity?.Name
                },
                cancellationToken);

            return BadRequest(new
            {
                status = 400,
                type = "https://httpstatuses.io/400/validation-error",
                title = "Validation error",
                detail = "One or more validation errors occurred.",
                instance = HttpContext.Request.Path.Value,
                correlationId = HttpContext.TraceIdentifier,
                errors = new Dictionary<string, string[]>
                {
                    ["File"] = new[] { "File is required" }
                }
            });
        }

        // Call use case with file stream
        await using var fileStream = file.OpenReadStream();
        var result = await uploadImageUseCase.ExecuteAsync(
            fileStream,
            file.FileName,
            file.ContentType,
            file.Length,
            "tips",
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to upload tip image: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.TipImageUploadFailed,
                null,
                SecurityEventOutcomes.Failure,
                HttpContext.TraceIdentifier,
                new Dictionary<string, string?>
                {
                    ["RoutePath"] = HttpContext.Request.Path,
                    ["FileName"] = file.FileName,
                    ["ContentType"] = file.ContentType,
                    ["FileSize"] = file.Length.ToString(),
                    ["ExceptionType"] = error.Type.ToString(),
                    ["AdminId"] = User.Identity?.Name
                },
                cancellationToken);

            return this.ToActionResult(error, HttpContext.TraceIdentifier);
        }

        var imageDto = result.Value!;

        logger.LogInformation(
            "Admin {AdminId} uploaded tip image. StoragePath: {StoragePath}, FileName: {FileName}, Size: {Size} bytes",
            User.Identity?.Name,
            imageDto.ImageStoragePath,
            imageDto.OriginalFileName,
            imageDto.FileSizeBytes);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.TipImageUploadSuccess,
            imageDto.ImageStoragePath,
            SecurityEventOutcomes.Success,
            HttpContext.TraceIdentifier,
            new Dictionary<string, string?>
            {
                ["RoutePath"] = HttpContext.Request.Path,
                ["StoragePath"] = imageDto.ImageStoragePath,
                ["FileName"] = imageDto.OriginalFileName,
                ["ContentType"] = imageDto.ContentType,
                ["FileSize"] = imageDto.FileSizeBytes.ToString(),
                ["AdminId"] = User.Identity?.Name
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(UploadTipImage),
            new { storagePath = imageDto.ImageStoragePath },
            imageDto);
    }
}
