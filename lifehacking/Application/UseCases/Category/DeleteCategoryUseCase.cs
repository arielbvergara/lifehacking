using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for soft-deleting a category and cascading the delete to associated tips.
/// </summary>
public class DeleteCategoryUseCase(
    ICategoryRepository categoryRepository,
    ITipRepository tipRepository)
{
    /// <summary>
    /// Executes the use case to soft-delete a category and all associated tips.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or an application exception.</returns>
    public async Task<Result<bool, AppException>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categoryId = CategoryId.Create(id);

            // Get existing category (returns null for soft-deleted categories)
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
            {
                return Result<bool, AppException>.Fail(
                    new NotFoundException("Category", id));
            }

            // Get all tips associated with this category
            var tips = await tipRepository.GetByCategoryAsync(categoryId, cancellationToken);

            // Mark category as deleted
            category.MarkDeleted();
            await categoryRepository.UpdateAsync(category, cancellationToken);

            // Mark all associated tips as deleted (cascade soft-delete)
            foreach (var tip in tips)
            {
                tip.MarkDeleted();
                await tipRepository.UpdateAsync(tip, cancellationToken);
            }

            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<bool, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(
                new InfraException("An unexpected error occurred while deleting the category", ex));
        }
    }
}
