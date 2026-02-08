using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
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

            // Check uniqueness for new name (case-insensitive, including soft-deleted, excluding current category)
            var existingCategory = await categoryRepository.GetByNameAsync(request.Name, includeDeleted: true, cancellationToken);
            if (existingCategory is not null && existingCategory.Id != categoryId)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new ConflictException($"Category with name '{request.Name}' already exists"));
            }

            // Update category name (validation happens in UpdateName)
            category.UpdateName(request.Name);

            // Save to repository
            await categoryRepository.UpdateAsync(category, cancellationToken);

            // Return response
            return Result<CategoryResponse, AppException>.Ok(category.ToCategoryResponse());
        }
        catch (AppException ex)
        {
            return Result<CategoryResponse, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<CategoryResponse, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<CategoryResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while updating the category", ex));
        }
    }
}
