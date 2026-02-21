using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for soft-deleting a tip.
/// </summary>
public class DeleteTipUseCase(
    ITipRepository tipRepository,
    ICacheInvalidationService cacheInvalidationService)
{
    /// <summary>
    /// Executes the use case to soft-delete a tip.
    /// </summary>
    /// <param name="id">The ID of the tip to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="NotFoundException"/> if the tip does not exist or is already soft-deleted.</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// </remarks>
    public async Task<Result<bool, AppException>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Load existing tip via ITipRepository.GetByIdAsync()
            var tipId = TipId.Create(id);
            var tip = await tipRepository.GetByIdAsync(tipId, cancellationToken);

            if (tip == null)
            {
                return Result<bool, AppException>.Fail(
                    new NotFoundException($"Tip with ID '{id}' not found"));
            }

            // 2. Mark tip as deleted via tip.MarkDeleted()
            tip.MarkDeleted();

            // 3. Persist via ITipRepository.UpdateAsync()
            await tipRepository.UpdateAsync(tip, cancellationToken);

            // 4. Invalidate category list and individual category cache
            cacheInvalidationService.InvalidateCategoryAndList(tip.CategoryId);

            // 5. Return success result
            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(
                new InfraException("An unexpected error occurred while deleting the tip", ex));
        }
    }
}
