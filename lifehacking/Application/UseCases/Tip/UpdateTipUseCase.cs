using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validation;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for updating an existing tip.
/// </summary>
public class UpdateTipUseCase(
    ITipRepository tipRepository,
    ICategoryRepository categoryRepository,
    ICacheInvalidationService cacheInvalidationService)
{
    /// <summary>
    /// Executes the use case to update an existing tip.
    /// </summary>
    /// <param name="tipId">The ID of the tip to update.</param>
    /// <param name="request">The update tip request containing updated tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the updated tip response or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="NotFoundException"/> if the tip does not exist or is soft-deleted.</description></item>
    /// <item><description>Returns <see cref="ValidationException"/> with field-level errors if any field fails validation (title, description, steps, tags, video URL, image).</description></item>
    /// <item><description>Returns <see cref="NotFoundException"/> if the specified category does not exist or is soft-deleted.</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// Multiple validation failures are aggregated into a single ValidationException with field-level detail.
    /// Image validation errors are prefixed with "Image." to indicate the nested field path.
    /// </remarks>
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
                    new NotFoundException($"Tip with ID '{tipId}' not found"));
            }

            // 2. Validate and create value objects using ValidationErrorBuilder
            var validationBuilder = new ValidationErrorBuilder();
            TipTitle? title = null;
            TipDescription? description = null;
            var steps = new List<TipStep>();
            var tags = new List<Tag>();
            VideoUrl? videoUrl = null;
            TipImage? image = null;

            // Validate title
            try
            {
                title = TipTitle.Create(request.Title);
            }
            catch (ArgumentException ex)
            {
                validationBuilder.AddError(nameof(request.Title), ex.Message);
            }

            // Validate description
            try
            {
                description = TipDescription.Create(request.Description);
            }
            catch (ArgumentException ex)
            {
                validationBuilder.AddError(nameof(request.Description), ex.Message);
            }

            // Validate steps collection
            if (request.Steps == null || request.Steps.Count == 0)
            {
                validationBuilder.AddError(nameof(request.Steps), "At least one step is required");
            }
            else
            {
                // Validate each step
                for (int i = 0; i < request.Steps.Count; i++)
                {
                    try
                    {
                        steps.Add(TipStep.Create(request.Steps[i].StepNumber, request.Steps[i].Description));
                    }
                    catch (ArgumentException ex)
                    {
                        validationBuilder.AddError($"{nameof(request.Steps)}[{i}]", ex.Message);
                    }
                }
            }

            // Validate tags (optional)
            if (request.Tags != null)
            {
                for (int i = 0; i < request.Tags.Count; i++)
                {
                    try
                    {
                        tags.Add(Tag.Create(request.Tags[i]));
                    }
                    catch (ArgumentException ex)
                    {
                        validationBuilder.AddError($"{nameof(request.Tags)}[{i}]", ex.Message);
                    }
                }
            }

            // Validate video URL (optional)
            if (!string.IsNullOrWhiteSpace(request.VideoUrl))
            {
                try
                {
                    videoUrl = VideoUrl.Create(request.VideoUrl);
                }
                catch (ArgumentException ex)
                {
                    validationBuilder.AddError(nameof(request.VideoUrl), ex.Message);
                }
            }

            // Validate image (optional)
            if (request.Image != null)
            {
                try
                {
                    image = request.Image.ToTipImage();
                }
                catch (ArgumentException ex)
                {
                    var fieldName = MapImageExceptionToFieldName(ex.ParamName);
                    validationBuilder.AddError(fieldName, ex.Message);
                }
            }

            // Return early if validation errors exist
            if (validationBuilder.HasErrors)
            {
                return Result<TipDetailResponse, AppException>.Fail(validationBuilder.Build());
            }

            // 3. Check if category exists
            var categoryId = CategoryId.Create(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new NotFoundException($"Category with ID '{request.CategoryId}' not found"));
            }

            // 4. Validate category is not soft-deleted
            if (category.IsDeleted)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new NotFoundException($"Category with ID '{request.CategoryId}' not found"));
            }

            // 5. Update tip entity via Update*() methods
            var oldCategoryId = tip.CategoryId;
            tip.UpdateTitle(title!);
            tip.UpdateDescription(description!);
            tip.UpdateSteps(steps);
            tip.UpdateCategory(categoryId);
            tip.UpdateTags(tags);
            tip.UpdateVideoUrl(videoUrl);
            tip.UpdateImage(image);

            // 6. Persist via repository
            await tipRepository.UpdateAsync(tip, cancellationToken);

            // 7. Invalidate caches for affected categories
            cacheInvalidationService.InvalidateCategoryAndList(categoryId);
            if (oldCategoryId != categoryId)
            {
                // If category changed, also invalidate the old category
                cacheInvalidationService.InvalidateCategory(oldCategoryId);
            }

            // 8. Map to response DTO and return success result
            var response = tip.ToTipDetailResponse(category.Name);
            return Result<TipDetailResponse, AppException>.Ok(response);
        }
        catch (AppException ex)
        {
            return Result<TipDetailResponse, AppException>.Fail(ex);
        }
        catch (Exception ex)
        {
            return Result<TipDetailResponse, AppException>.Fail(
                new InfraException("An unexpected error occurred while updating the tip", ex));
        }
    }

    /// <summary>
    /// Maps TipImage value object parameter names to request field names with "Image." prefix.
    /// </summary>
    private static string MapImageExceptionToFieldName(string? paramName)
    {
        return paramName switch
        {
            "imageUrl" => "Image.ImageUrl",
            "imageStoragePath" => "Image.ImageStoragePath",
            "originalFileName" => "Image.OriginalFileName",
            "contentType" => "Image.ContentType",
            "fileSizeBytes" => "Image.FileSizeBytes",
            _ => "Image"
        };
    }
}
