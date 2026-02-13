namespace Application.Dtos.Tip;

public static class TipExtensions
{
    extension(Domain.Entities.Tip tip)
    {
        public TipSummaryResponse ToTipSummaryResponse(string categoryName)
        {
            ArgumentNullException.ThrowIfNull(tip);
            ArgumentNullException.ThrowIfNull(categoryName);

            return new TipSummaryResponse(
                tip.Id.Value,
                tip.Title.Value,
                tip.Description.Value,
                tip.CategoryId.Value,
                categoryName,
                tip.Tags.Select(t => t.Value).ToList(),
                tip.VideoUrl?.Value,
                tip.CreatedAt
            );
        }

        public TipDetailResponse ToTipDetailResponse(string categoryName)
        {
            ArgumentNullException.ThrowIfNull(tip);
            ArgumentNullException.ThrowIfNull(categoryName);

            return new TipDetailResponse(
                tip.Id.Value,
                tip.Title.Value,
                tip.Description.Value,
                tip.Steps.Select(s => new TipStepDto(s.StepNumber, s.Description)).ToList(),
                tip.CategoryId.Value,
                categoryName,
                tip.Tags.Select(t => t.Value).ToList(),
                tip.VideoUrl?.Value,
                tip.VideoUrl?.VideoId,
                tip.CreatedAt,
                tip.UpdatedAt,
                tip.Image?.ToTipImageDto()
            );
        }
    }

    public static TipImageDto ToTipImageDto(this Domain.ValueObject.TipImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new TipImageDto(
            image.ImageUrl,
            image.ImageStoragePath,
            image.OriginalFileName,
            image.ContentType,
            image.FileSizeBytes,
            image.UploadedAt
        );
    }

    public static Domain.ValueObject.TipImage? ToTipImage(this TipImageDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return Domain.ValueObject.TipImage.Create(
            dto.ImageUrl,
            dto.ImageStoragePath,
            dto.OriginalFileName,
            dto.ContentType,
            dto.FileSizeBytes,
            dto.UploadedAt
        );
    }
}
