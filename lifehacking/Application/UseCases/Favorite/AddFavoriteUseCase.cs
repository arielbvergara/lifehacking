using Application.Dtos.Favorite;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Primitives;

namespace Application.UseCases.Favorite;

/// <summary>
/// Use case for adding a tip to a user's favorites.
/// Validates that both user and tip exist, and that the favorite doesn't already exist.
/// </summary>
public class AddFavoriteUseCase(
    IFavoritesRepository favoritesRepository,
    ITipRepository tipRepository,
    IUserRepository userRepository,
    ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Executes the add favorite operation.
    /// </summary>
    /// <param name="request">The request containing user ID and tip ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the favorite response or an application exception.</returns>
    public async Task<Result<FavoriteResponse, AppException>> ExecuteAsync(
        AddFavoriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Validate user exists
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result<FavoriteResponse, AppException>.Fail(
                    new NotFoundException($"User with ID '{request.UserId.Value}' not found."));
            }

            // Validate tip exists
            var tip = await tipRepository.GetByIdAsync(request.TipId, cancellationToken);
            if (tip is null)
            {
                return Result<FavoriteResponse, AppException>.Fail(
                    new NotFoundException($"Tip with ID '{request.TipId.Value}' not found."));
            }

            // Check if favorite already exists
            var existingFavorite = await favoritesRepository.GetByUserAndTipAsync(
                request.UserId,
                request.TipId,
                cancellationToken);

            if (existingFavorite is not null)
            {
                return Result<FavoriteResponse, AppException>.Fail(
                    new ConflictException($"Tip '{request.TipId.Value}' is already in user's favorites."));
            }

            // Create and add the favorite
            var favorite = UserFavorites.Create(request.UserId, request.TipId);
            var addedFavorite = await favoritesRepository.AddAsync(favorite, cancellationToken);

            // Get category name for response
            var category = await GetCategoryNameAsync(tip.CategoryId, cancellationToken);

            // Convert to response DTO
            var response = addedFavorite.ToFavoriteResponse(tip, category);

            return Result<FavoriteResponse, AppException>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<FavoriteResponse, AppException>.Fail(
                new InfraException("An error occurred while adding the favorite.", ex));
        }
    }

    private async Task<string> GetCategoryNameAsync(
        Domain.ValueObject.CategoryId categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category?.Name ?? "Unknown Category";
    }
}
