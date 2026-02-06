using Application.Dtos;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Category;

/// <summary>
/// Use case for retrieving paginated and sorted tips belonging to a specific category.
/// </summary>
public class GetTipsByCategoryUseCase(
    ICategoryRepository categoryRepository,
    ITipRepository tipRepository)
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Executes the use case to retrieve tips for a specific category.
    /// </summary>
    /// <param name="categoryIdString">The category ID as a string.</param>
    /// <param name="request">The request containing pagination and sorting parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the paginated tips response or an application exception.</returns>
    public async Task<Result<PagedTipsResponse, AppException>> ExecuteAsync(
        string categoryIdString,
        GetTipsByCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categoryIdString);
        ArgumentNullException.ThrowIfNull(request);

        // Validate category ID format
        if (!Guid.TryParse(categoryIdString, out var categoryGuid))
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new ValidationException($"Invalid category ID format: '{categoryIdString}'. Expected a valid GUID."));
        }

        var categoryId = CategoryId.Create(categoryGuid);

        // Check if category exists
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new NotFoundException($"Category with ID '{categoryIdString}' not found"));
        }

        // Apply defaults and validate pagination parameters
        var pageNumber = request.PageNumber ?? DefaultPageNumber;
        var pageSize = request.PageSize ?? DefaultPageSize;

        if (pageNumber < 1)
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new ValidationException("Page number must be greater than or equal to 1"));
        }

        if (pageSize < MinPageSize || pageSize > MaxPageSize)
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new ValidationException($"Page size must be between {MinPageSize} and {MaxPageSize}"));
        }

        // Build query criteria with defaults
        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: categoryGuid,
            Tags: null,
            SortField: request.OrderBy ?? TipSortField.CreatedAt,
            SortDirection: request.SortDirection ?? SortDirection.Descending,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        try
        {
            // Retrieve paginated tips
            var (tips, totalCount) = await tipRepository.SearchAsync(criteria, cancellationToken);

            // Map tips to summary responses
            var tipSummaries = tips
                .Select(tip => tip.ToTipSummaryResponse(category.Name))
                .ToList();

            // Create pagination metadata
            var metadata = new PaginationMetadata(
                totalCount,
                pageNumber,
                pageSize,
                (int)Math.Ceiling((double)totalCount / pageSize)
            );

            return Result<PagedTipsResponse, AppException>.Ok(
                new PagedTipsResponse(tipSummaries, metadata));
        }
        catch (Exception ex)
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new InfraException("Failed to retrieve tips by category", ex));
        }
    }
}
