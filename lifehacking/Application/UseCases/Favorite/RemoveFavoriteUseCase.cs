using Application.Dtos.Favorite;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.Favorite;

/// <summary>
/// Use case for removing a tip from a user's favorites.
/// Validates that the user exists and that the favorite exists before removal.
/// </summary>
public class RemoveFavoriteUseCase(
    IFavoritesRepository favoritesRepository,
    IUserRepository userRepository)
{
    /// <summary>
    /// Executes the remove favorite operation.
    /// </summary>
    /// <param name="request">The request containing user ID and tip ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing true if removed successfully, or an application exception.</returns>
    public async Task<Result<bool, AppException>> ExecuteAsync(
        RemoveFavoriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Validate user exists
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result<bool, AppException>.Fail(
                    new NotFoundException($"User with ID '{request.UserId.Value}' not found."));
            }

            // Check if favorite exists
            var existingFavorite = await favoritesRepository.GetByUserAndTipAsync(
                request.UserId,
                request.TipId,
                cancellationToken);

            if (existingFavorite is null)
            {
                return Result<bool, AppException>.Fail(
                    new NotFoundException($"Tip '{request.TipId.Value}' not found in user's favorites."));
            }

            // Remove the favorite
            var removed = await favoritesRepository.RemoveAsync(
                request.UserId,
                request.TipId,
                cancellationToken);

            return Result<bool, AppException>.Ok(removed);
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(
                new InfraException("An error occurred while removing the favorite.", ex));
        }
    }
}
