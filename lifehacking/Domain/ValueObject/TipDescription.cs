namespace Domain.ValueObject;

public sealed record TipDescription
{
    public const int MinLength = 10;
    public const int MaxLength = 2000;

    public string Value { get; }

    private TipDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tip description cannot be empty", nameof(value));
        }

        if (value.Length < MinLength)
        {
            throw new ArgumentException($"Tip description must be at least {MinLength} characters", nameof(value));
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Tip description cannot exceed {MaxLength} characters", nameof(value));
        }

        Value = value.Trim();
    }

    public static TipDescription Create(string value) => new(value);
}
