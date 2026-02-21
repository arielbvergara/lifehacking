using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validation;
using Domain.Primitives;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for creating a new category.
/// </summary>
public class CreateCategoryUseCase(
    ICategoryRepository categoryRepository,
    ICacheInvalidationService cacheInvalidationService)
{
    /// <summary>
    /// Executes the use case to create a new category.
    /// </summary>
    /// <param name="request">The create category request containing the category name and optional image metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the created category response or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="ValidationException"/> with field-level errors if the name fails validation (empty, too short, too long).</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> with field-level errors if image metadata is provided and fails validation (invalid content type, file size exceeds maximum, invalid URL format, empty required fields).</description></item>
    /// <item><description>Returns <see cref="ConflictException"/> if a category with the same name already exists (case-insensitive, including soft-deleted).</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// </remarks>
    public async Task<Result<CategoryResponse, AppException>> ExecuteAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationBuilder = new ValidationErrorBuilder();
            Domain.Entities.Category? category = null;
            Domain.ValueObject.CategoryImage? image = null;

            // Validate and create image if provided
            if (request.Image is not null)
            {
                try
                {
                    image = request.Image.ToCategoryImage();
                }
                catch (ArgumentException ex)
                {
                    // Map exception parameter name to DTO field name
                    var fieldName = MapImageExceptionToFieldName(ex);
                    validationBuilder.AddError($"Image.{fieldName}", ex.Message);
                }
            }

            // Validate and create category
            try
            {
                category = Domain.Entities.Category.Create(request.Name, image);
            }
            catch (ArgumentException ex)
            {
                validationBuilder.AddError(nameof(request.Name), ex.Message);
            }

            // Return early if validation errors exist
            if (validationBuilder.HasErrors)
            {
                return Result<CategoryResponse, AppException>.Fail(validationBuilder.Build());
            }

            // Check uniqueness (case-insensitive, including soft-deleted categories)
            var existingCategory = await categoryRepository.GetByNameAsync(request.Name, includeDeleted: true, cancellationToken);
            if (existingCategory is not null)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new ConflictException($"Category with name '{request.Name}' already exists"));
            }

            // Save to repository
            await categoryRepository.AddAsync(category!, cancellationToken);

            // Invalidate category list cache
            cacheInvalidationService.InvalidateCategoryList();

            // Return response
            return Result<CategoryResponse, AppException>.Ok(category!.ToCategoryResponse(0));
        }
        catch (AppException ex)
        {
            return Result<CategoryResponse, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<CategoryResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while creating the category", ex));
        }
    }

    private static string MapImageExceptionToFieldName(ArgumentException ex)
    {
        // Map parameter name from exception to DTO field name
        return ex.ParamName switch
        {
            "imageUrl" => "ImageUrl",
            "imageStoragePath" => "ImageStoragePath",
            "originalFileName" => "OriginalFileName",
            "contentType" => "ContentType",
            "fileSizeBytes" => "FileSizeBytes",
            _ => "Image"
        };
    }
}
