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
                tip.CreatedAt,
                tip.Image?.ToImageDto()
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
                tip.Image?.ToImageDto()
            );
        }
    }
}
