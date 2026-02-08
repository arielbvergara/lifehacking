using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for updating an existing tip.
/// </summary>
public class UpdateTipUseCase(ITipRepository tipRepository, ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Executes the use case to update an existing tip.
    /// </summary>
    /// <param name="tipId">The ID of the tip to update.</param>
    /// <param name="request">The update tip request containing updated tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the updated tip response or an application exception.</returns>
    public async Task<Result<TipDetailResponse, AppException>> ExecuteAsync(
        Guid tipId,
        UpdateTipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Load existing tip
            var id = TipId.Create(tipId);
            var tip = await tipRepository.GetByIdAsync(id, cancellationToken);

            if (tip == null)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new NotFoundException("Tip", tipId));
            }

            // 2. Validate and create value objects
            var title = TipTitle.Create(request.Title);
            var description = TipDescription.Create(request.Description);

            // Create steps from request
            var steps = new List<TipStep>();
            if (request.Steps == null || request.Steps.Count == 0)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new ValidationException("At least one step is required"));
            }

            foreach (var stepRequest in request.Steps)
            {
                steps.Add(TipStep.Create(stepRequest.StepNumber, stepRequest.Description));
            }

            // Create tags from request (optional)
            var tags = new List<Tag>();
            if (request.Tags != null)
            {
                foreach (var tagValue in request.Tags)
                {
                    tags.Add(Tag.Create(tagValue));
                }
            }

            // Create video URL if provided (optional)
            VideoUrl? videoUrl = null;
            if (!string.IsNullOrWhiteSpace(request.YouTubeUrl))
            {
                videoUrl = VideoUrl.Create(request.YouTubeUrl);
            }

            // 3. Check if category exists
            var categoryId = CategoryId.Create(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new ValidationException("Category does not exist"));
            }

            // 4. Validate category is not soft-deleted
            if (category.IsDeleted)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new ValidationException("Cannot assign tip to a deleted category"));
            }

            // 5. Update tip entity via Update*() methods
            tip.UpdateTitle(title);
            tip.UpdateDescription(description);
            tip.UpdateSteps(steps);
            tip.UpdateCategory(categoryId);
            tip.UpdateTags(tags);
            tip.UpdateYouTubeUrl(videoUrl);

            // 6. Persist via repository
            await tipRepository.UpdateAsync(tip, cancellationToken);

            // 7. Map to response DTO and return success result
            var response = tip.ToTipDetailResponse(category.Name);
            return Result<TipDetailResponse, AppException>.Ok(response);
        }
        catch (AppException ex)
        {
            return Result<TipDetailResponse, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<TipDetailResponse, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<TipDetailResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while updating the tip", ex));
        }
    }
}
