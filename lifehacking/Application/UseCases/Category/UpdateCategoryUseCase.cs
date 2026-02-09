using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validation;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for updating an existing category.
/// </summary>
public class UpdateCategoryUseCase(ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Executes the use case to update a category's name.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="request">The update category request containing the new name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the updated category response or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="NotFoundException"/> if the category does not exist or is soft-deleted.</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> with field-level errors if the name fails validation (empty, too short, too long).</description></item>
    /// <item><description>Returns <see cref="ConflictException"/> if another category with the same name already exists (case-insensitive, including soft-deleted).</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// </remarks>
    public async Task<Result<CategoryResponse, AppException>> ExecuteAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categoryId = CategoryId.Create(id);

            // Get existing category (returns null for soft-deleted categories)
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new NotFoundException("Category", id));
            }

            // Check uniqueness for new name BEFORE updating (case-insensitive, including soft-deleted, excluding current category)
            // This must happen before UpdateName to avoid race conditions with the duplicate check
            var existingCategory = await categoryRepository.GetByNameAsync(request.Name, includeDeleted: true, cancellationToken);
            if (existingCategory is not null && existingCategory.Id != categoryId)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new ConflictException($"Category with name '{request.Name}' already exists"));
            }

            // Validate new name using ValidationErrorBuilder
            var validationBuilder = new ValidationErrorBuilder();

            try
            {
                category.UpdateName(request.Name);
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

            // Save to repository
            await categoryRepository.UpdateAsync(category, cancellationToken);

            // Return response
            return Result<CategoryResponse, AppException>.Ok(category.ToCategoryResponse());
        }
        catch (AppException ex)
        {
            return Result<CategoryResponse, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<CategoryResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while updating the category", ex));
        }
    }
}
