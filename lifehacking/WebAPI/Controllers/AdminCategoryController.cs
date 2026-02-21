using Application.Caching;
using Application.Dtos.Category;
using Application.Interfaces;
using Application.UseCases.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
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
    UploadCategoryImageUseCase uploadCategoryImageUseCase,
    IMemoryCache memoryCache,
    ISecurityEventNotifier securityEventNotifier,
    ILogger<AdminCategoryController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <remarks>
    /// This endpoint is restricted to administrators. Creates a new category with the specified name.
    ///
    /// **Validation Rules:**
    /// - **Name**: Required, 2-100 characters (trimmed), case-insensitive uniqueness check
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
    ///   "instance": "/api/admin/categories",
    ///   "correlationId": "abc123",
    ///   "errors": {
    ///     "Name": ["Category name must be at least 2 characters"]
    ///   }
    /// }
    /// ```
    ///
    /// **409 Conflict** - Category name already exists (case-insensitive):
    /// ```json
    /// {
    ///   "status": 409,
    ///   "type": "https://httpstatuses.io/409/conflict",
    ///   "title": "Conflict",
    ///   "detail": "Category with name 'Productivity' already exists",
    ///   "instance": "/api/admin/categories",
    ///   "correlationId": "def456"
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
    ///   "instance": "/api/admin/categories",
    ///   "correlationId": "ghi789"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
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
    ///
    /// **Validation Rules:**
    /// - **Name**: Required, 2-100 characters (trimmed), case-insensitive uniqueness check
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
    ///   "instance": "/api/admin/categories/{id}",
    ///   "correlationId": "abc123",
    ///   "errors": {
    ///     "Name": ["Category name cannot exceed 100 characters"]
    ///   }
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
    ///   "instance": "/api/admin/categories/123e4567-e89b-12d3-a456-426614174000",
    ///   "correlationId": "def456"
    /// }
    /// ```
    ///
    /// **409 Conflict** - Category name already exists (case-insensitive):
    /// ```json
    /// {
    ///   "status": 409,
    ///   "type": "https://httpstatuses.io/409/conflict",
    ///   "title": "Conflict",
    ///   "detail": "Category with name 'Productivity' already exists",
    ///   "instance": "/api/admin/categories/{id}",
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
    ///   "instance": "/api/admin/categories/{id}",
    ///   "correlationId": "jkl012"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
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

        // Invalidate cache for this category
        var cacheKey = CacheKeys.Category(category.Id);
        memoryCache.Remove(cacheKey);

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
    ///
    /// **Possible Error Responses:**
    ///
    /// **404 Not Found** - Category does not exist or is already soft-deleted:
    /// ```json
    /// {
    ///   "status": 404,
    ///   "type": "https://httpstatuses.io/404/resource-not-found",
    ///   "title": "Resource not found",
    ///   "detail": "Category with id '123e4567-e89b-12d3-a456-426614174000' was not found.",
    ///   "instance": "/api/admin/categories/123e4567-e89b-12d3-a456-426614174000",
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
    ///   "instance": "/api/admin/categories/{id}",
    ///   "correlationId": "def456"
    /// }
    /// ```
    ///
    /// All error responses include a `correlationId` for tracing and follow RFC 7807 Problem Details format.
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

        // Invalidate cache for this category
        var cacheKey = CacheKeys.Category(id);
        memoryCache.Remove(cacheKey);

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

    /// <summary>
    /// Uploads a category image to AWS S3 storage.
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
    ///   "instance": "/api/admin/categories/images",
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
    ///   "instance": "/api/admin/categories/images",
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
    [ProducesResponseType<CategoryImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadCategoryImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file is provided
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Image upload failed: No file provided");

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.CategoryImageUploadFailed,
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
        var result = await uploadCategoryImageUseCase.ExecuteAsync(
            fileStream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;
            logger.LogError(error.InnerException, "Failed to upload category image: {Message}", error.Message);

            await securityEventNotifier.NotifyAsync(
                SecurityEventNames.CategoryImageUploadFailed,
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
            "Admin {AdminId} uploaded category image. StoragePath: {StoragePath}, FileName: {FileName}, Size: {Size} bytes",
            User.Identity?.Name,
            imageDto.ImageStoragePath,
            imageDto.OriginalFileName,
            imageDto.FileSizeBytes);

        await securityEventNotifier.NotifyAsync(
            SecurityEventNames.CategoryImageUploadSuccess,
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
            nameof(UploadCategoryImage),
            new { storagePath = imageDto.ImageStoragePath },
            imageDto);
    }
}
