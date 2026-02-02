namespace Domain.ValueObject;

public sealed record TipStep
{
    private const int MinDescriptionLength = 10;
    private const int MaxDescriptionLength = 500;

    public int StepNumber { get; }
    public string Description { get; }

    private TipStep(int stepNumber, string description)
    {
        if (stepNumber < 1)
        {
            throw new ArgumentException("Step number must be at least 1", nameof(stepNumber));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Step description cannot be empty", nameof(description));
        }

        var trimmedDescription = description.Trim();

        if (trimmedDescription.Length < MinDescriptionLength)
        {
            throw new ArgumentException($"Step description must be at least {MinDescriptionLength} characters", nameof(description));
        }

        if (trimmedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Step description cannot exceed {MaxDescriptionLength} characters", nameof(description));
        }

        StepNumber = stepNumber;
        Description = trimmedDescription;
    }

    public static TipStep Create(int stepNumber, string description) => new(stepNumber, description);
}
