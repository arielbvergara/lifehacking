using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for retrieving all non-deleted categories.
/// </summary>
public class GetCategoriesUseCase(ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Executes the use case to retrieve all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the category list response or an application exception.</returns>
    public async Task<Result<CategoryListResponse, AppException>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await categoryRepository.GetAllAsync(cancellationToken);

            var categoryResponses = categories
                .Select(c => c.ToCategoryResponse())
                .ToList();

            return Result<CategoryListResponse, AppException>.Ok(
                new CategoryListResponse(categoryResponses));
        }
        catch (Exception ex)
        {
            return Result<CategoryListResponse, AppException>.Fail(
                new InfraException("Failed to retrieve categories", ex));
        }
    }
}
