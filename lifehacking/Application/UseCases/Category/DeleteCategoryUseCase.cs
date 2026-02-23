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
    ITipRepository tipRepository,
    ICacheInvalidationService cacheInvalidationService)
{
    /// <summary>
    /// Executes the use case to soft-delete a category and all associated tips.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="NotFoundException"/> if the category does not exist or is already soft-deleted.</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// This operation cascades the soft-delete to all tips associated with the category.
    /// </remarks>
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

            // Invalidate category list and individual category cache
            cacheInvalidationService.InvalidateCategoryAndList(categoryId);
            cacheInvalidationService.InvalidateDashboard();

            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(
                new InfraException("An unexpected error occurred while deleting the category", ex));
        }
    }
}
