using Application.Dtos.Favorite;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Favorite;

/// <summary>
/// Use case for merging client-stored anonymous favorites into server-side favorites.
/// Performs validation, deduplication, and batch operations for efficient merging.
/// </summary>
public sealed class MergeFavoritesUseCase(
    IFavoritesRepository favoritesRepository,
    ITipRepository tipRepository,
    IUserRepository userRepository)
{
    private readonly IFavoritesRepository _favoritesRepository = favoritesRepository ?? throw new ArgumentNullException(nameof(favoritesRepository));
    private readonly ITipRepository _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    /// <summary>
    /// Executes the merge favorites operation.
    /// </summary>
    /// <param name="request">The merge request containing user ID and tip IDs to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the merge summary or an error.</returns>
    public async Task<Result<MergeFavoritesResponse, AppException>> ExecuteAsync(
        MergeFavoritesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result<MergeFavoritesResponse, AppException>.Fail(
                    new NotFoundException($"User with ID '{request.UserId.Value}' not found."));
            }

            // 2. Track original count before deduplication
            var totalReceived = request.TipIds.Count;

            // 3. Deduplicate input tip IDs
            var uniqueTipIds = request.TipIds.ToHashSet();

            // Handle empty input
            if (uniqueTipIds.Count == 0)
            {
                return Result<MergeFavoritesResponse, AppException>.Ok(
                    new MergeFavoritesResponse(
                        TotalReceived: totalReceived,
                        Added: 0,
                        Skipped: 0,
                        Failed: Array.Empty<FailedTip>()));
            }

            // 4. Batch validate tips exist
            var validTips = await _tipRepository.GetByIdsAsync(uniqueTipIds, cancellationToken);

            // 5. Separate valid tip IDs from invalid/non-existent ones
            var validTipIds = validTips.Keys.ToHashSet();
            var invalidTipIds = uniqueTipIds.Except(validTipIds).ToList();

            // Build failed list for invalid/non-existent tips
            var failedTips = invalidTipIds
                .Select(tipId => new FailedTip(tipId.Value, "Tip not found"))
                .ToList();

            // If no valid tips, return early
            if (validTipIds.Count == 0)
            {
                return Result<MergeFavoritesResponse, AppException>.Ok(
                    new MergeFavoritesResponse(
                        TotalReceived: totalReceived,
                        Added: 0,
                        Skipped: 0,
                        Failed: failedTips));
            }

            // 6. Query existing favorites
            var existingFavorites = await _favoritesRepository.GetExistingFavoritesAsync(
                request.UserId,
                validTipIds,
                cancellationToken);

            // 7. Calculate new tips to add (valid tips - existing favorites)
            var newTipsToAdd = validTipIds.Except(existingFavorites).ToList();

            // 8. Batch add new favorites
            if (newTipsToAdd.Count > 0)
            {
                await _favoritesRepository.AddBatchAsync(
                    request.UserId,
                    newTipsToAdd,
                    cancellationToken);
            }

            // 9. Build response summary
            var response = new MergeFavoritesResponse(
                TotalReceived: totalReceived,
                Added: newTipsToAdd.Count,
                Skipped: existingFavorites.Count,
                Failed: failedTips);

            return Result<MergeFavoritesResponse, AppException>.Ok(response);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            // Wrap infrastructure exceptions
            return Result<MergeFavoritesResponse, AppException>.Fail(
                new InfraException("An error occurred while merging favorites.", ex));
        }
    }
}
