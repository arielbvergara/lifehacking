using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for retrieving a single category by its unique identifier.
/// </summary>
/// <summary>
/// Use case for retrieving a single category by its unique identifier.
/// </summary>
public class GetCategoryByIdUseCase(
    ICategoryRepository categoryRepository,
    ITipRepository tipRepository)
{
    /// <summary>
    /// Executes the use case to retrieve a category by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the category response or an application exception.</returns>
    public async Task<Result<CategoryResponse, AppException>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create CategoryId value object from GUID
            var categoryId = CategoryId.Create(id);

            // Retrieve category from repository
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            // Return NotFoundException if category doesn't exist
            if (category is null)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new NotFoundException("Category", id));
            }

            // Return NotFoundException if category is deleted
            if (category.IsDeleted)
            {
                return Result<CategoryResponse, AppException>.Fail(
                    new NotFoundException("Category", id));
            }

            // Get tips for this category to count them
            var tips = await tipRepository.GetByCategoryAsync(categoryId, cancellationToken);
            var tipCount = tips.Count(t => !t.IsDeleted);

            // Map Category entity to CategoryResponse with tip count
            var response = category.ToCategoryResponse(tipCount);

            return Result<CategoryResponse, AppException>.Ok(response);
        }
        catch (AppException)
        {
            // Re-throw AppExceptions (including NotFoundException)
            throw;
        }
        catch (Exception ex)
        {
            // Wrap infrastructure exceptions in InfraException
            return Result<CategoryResponse, AppException>.Fail(
                new InfraException("Failed to retrieve category", ex));
        }
    }
}
