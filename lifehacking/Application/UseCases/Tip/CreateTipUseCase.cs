using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for creating a new tip.
/// </summary>
public class CreateTipUseCase(ITipRepository tipRepository, ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Executes the use case to create a new tip.
    /// </summary>
    /// <param name="request">The create tip request containing tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the created tip response or an application exception.</returns>
    public async Task<Result<TipDetailResponse, AppException>> ExecuteAsync(
        CreateTipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate and create value objects
            var title = TipTitle.Create(request.Title);
            var description = TipDescription.Create(request.Description);

            // Create steps from request
            var steps = new List<TipStep>();
            if (request.Steps.Count == 0)
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

            // 2. Check if category exists
            var categoryId = CategoryId.Create(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new ValidationException("Category does not exist"));
            }

            // 3. Validate category is not soft-deleted
            if (category.IsDeleted)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new ValidationException("Cannot assign tip to a deleted category"));
            }

            // 4. Create Tip entity
            var tip = Domain.Entities.Tip.Create(
                title,
                description,
                steps,
                categoryId,
                tags,
                videoUrl);

            // 5. Persist via repository
            await tipRepository.AddAsync(tip, cancellationToken);

            // 6. Map to response DTO and return success result
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
                new InfraException("An unexpected error occurred while creating the tip", ex));
        }
    }
}
