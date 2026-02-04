using Application.Dtos.Tip;
using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.Tip;

public class SearchTipsUseCase(ITipRepository tipRepository, ICategoryRepository categoryRepository)
{
    public async Task<Result<PagedTipsResponse, AppException>> ExecuteAsync(
        SearchTipsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var (tips, totalCount) = await tipRepository.SearchAsync(request.Criteria, cancellationToken);

            // Get all unique category IDs from the tips
            var categoryIds = tips.Select(t => t.CategoryId).Distinct().ToList();

            // Fetch all categories in a single batch to avoid N+1 queries
            var categories = new Dictionary<Domain.ValueObject.CategoryId, string>();
            foreach (var categoryId in categoryIds)
            {
                var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
                if (category is not null)
                {
                    categories[categoryId] = category.Name;
                }
            }

            // Map tips to summary responses
            var tipSummaries = tips.Select(tip =>
            {
                var categoryName = categories.TryGetValue(tip.CategoryId, out var name) ? name : "Unknown Category";
                return tip.ToTipSummaryResponse(categoryName);
            }).ToList();

            // Create pagination metadata
            var metadata = new PaginationMetadata(
                totalCount,
                request.Criteria.PageNumber,
                request.Criteria.PageSize,
                (int)Math.Ceiling((double)totalCount / request.Criteria.PageSize)
            );

            var response = new PagedTipsResponse(tipSummaries, metadata);

            return Result<PagedTipsResponse, AppException>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<PagedTipsResponse, AppException>.Fail(
                new InfraException("An error occurred while searching for tips.", ex));
        }
    }
}
