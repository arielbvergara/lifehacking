using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validation;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

/// <summary>
/// Use case for creating a new tip.
/// </summary>
public class CreateTipUseCase(
    ITipRepository tipRepository,
    ICategoryRepository categoryRepository,
    ICacheInvalidationService cacheInvalidationService)
{
    /// <summary>
    /// Executes the use case to create a new tip.
    /// </summary>
    /// <param name="request">The create tip request containing tip details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the created tip response or an application exception.</returns>
    /// <remarks>
    /// Error handling:
    /// <list type="bullet">
    /// <item><description>Returns <see cref="ValidationException"/> with field-level errors if any field fails validation (title, description, steps, tags, video URL, image).</description></item>
    /// <item><description>Returns <see cref="NotFoundException"/> if the specified category does not exist or is soft-deleted.</description></item>
    /// <item><description>Returns <see cref="InfraException"/> if an unexpected error occurs during persistence.</description></item>
    /// </list>
    /// Multiple validation failures are aggregated into a single ValidationException with field-level detail.
    /// Image validation errors are prefixed with "Image." to indicate the nested field path.
    /// </remarks>
    public async Task<Result<TipDetailResponse, AppException>> ExecuteAsync(
        CreateTipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate and create value objects using ValidationErrorBuilder
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

            // 2. Check if category exists
            var categoryId = CategoryId.Create(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new NotFoundException($"Category with ID '{request.CategoryId}' not found"));
            }

            // 3. Validate category is not soft-deleted
            if (category.IsDeleted)
            {
                return Result<TipDetailResponse, AppException>.Fail(
                    new NotFoundException($"Category with ID '{request.CategoryId}' not found"));
            }

            // 4. Create Tip entity
            var tip = Domain.Entities.Tip.Create(
                title!,
                description!,
                steps,
                categoryId,
                tags,
                videoUrl,
                image);

            // 5. Persist via repository
            await tipRepository.AddAsync(tip, cancellationToken);

            // 6. Invalidate category list and individual category cache
            cacheInvalidationService.InvalidateCategoryAndList(categoryId);

            // 7. Map to response DTO and return success result
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
                new InfraException("An unexpected error occurred while creating the tip", ex));
        }
    }

    /// <summary>
    /// Maps TipImage value object parameter names to request field names with "Image." prefix.
    /// </summary>
    private static string MapImageExceptionToFieldName(string? paramName)
    {
        return paramName switch
        {
            "imageUrl" => $"{nameof(CreateTipRequest.Image)}.{nameof(TipImageDto.ImageUrl)}",
            "imageStoragePath" => $"{nameof(CreateTipRequest.Image)}.{nameof(TipImageDto.ImageStoragePath)}",
            "originalFileName" => $"{nameof(CreateTipRequest.Image)}.{nameof(TipImageDto.OriginalFileName)}",
            "contentType" => $"{nameof(CreateTipRequest.Image)}.{nameof(TipImageDto.ContentType)}",
            "fileSizeBytes" => $"{nameof(CreateTipRequest.Image)}.{nameof(TipImageDto.FileSizeBytes)}",
            _ => nameof(CreateTipRequest.Image)
        };
    }
}

