using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for soft-deleting a tip.
/// </summary>
public class DeleteTipUseCase(ITipRepository tipRepository)
{
    /// <summary>
    /// Executes the use case to soft-delete a tip.
    /// </summary>
    /// <param name="id">The ID of the tip to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or an application exception.</returns>
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
                    new NotFoundException("Tip", id));
            }

            // 2. Mark tip as deleted via tip.MarkDeleted()
            tip.MarkDeleted();

            // 3. Persist via ITipRepository.UpdateAsync()
            await tipRepository.UpdateAsync(tip, cancellationToken);

            // 4. Return success result
            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<bool, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(
                new InfraException("An unexpected error occurred while deleting the tip", ex));
        }
    }
}
