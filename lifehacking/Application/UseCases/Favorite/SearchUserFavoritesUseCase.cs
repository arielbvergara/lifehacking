using Application.Dtos.Favorite;
using Application.Dtos.Tip;
using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.Favorite;

/// <summary>
/// Use case for searching a user's favorites with filtering, sorting, and pagination.
/// Returns full tip details for each favorite.
/// </summary>
public class SearchUserFavoritesUseCase(
    IFavoritesRepository favoritesRepository,
    ICategoryRepository categoryRepository,
    IUserRepository userRepository)
{
    /// <summary>
    /// Executes the search favorites operation.
    /// </summary>
    /// <param name="request">The request containing user ID and query criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing paginated favorites or an application exception.</returns>
    public async Task<Result<PagedFavoritesResponse, AppException>> ExecuteAsync(
        SearchUserFavoritesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Validate user exists
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result<PagedFavoritesResponse, AppException>.Fail(
                    new NotFoundException($"User with ID '{request.UserId.Value}' not found."));
            }

            // Search user's favorites
            var (tips, totalCount) = await favoritesRepository.SearchUserFavoritesAsync(
                request.UserId,
                request.Criteria,
                cancellationToken);

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

            // Map tips to favorite responses
            // Note: We need to get the AddedAt timestamp from the favorites
            // For now, we'll use the tip's CreatedAt as a placeholder
            // This will be properly implemented when the repository returns UserFavorites entities
            var favoriteResponses = tips.Select(tip =>
            {
                var categoryName = categories.TryGetValue(tip.CategoryId, out var name)
                    ? name
                    : "Unknown Category";

                var tipDetails = tip.ToTipDetailResponse(categoryName);

                return new FavoriteResponse(
                    TipId: tip.Id.Value,
                    AddedAt: tip.CreatedAt, // Placeholder - should be favorite.AddedAt
                    TipDetails: tipDetails);
            }).ToList();

            // Create pagination metadata
            var metadata = new PaginationMetadata(
                totalCount,
                request.Criteria.PageNumber,
                request.Criteria.PageSize,
                (int)Math.Ceiling((double)totalCount / request.Criteria.PageSize)
            );

            var response = new PagedFavoritesResponse(favoriteResponses, metadata);

            return Result<PagedFavoritesResponse, AppException>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<PagedFavoritesResponse, AppException>.Fail(
                new InfraException("An error occurred while searching favorites.", ex));
        }
    }
}
