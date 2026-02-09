namespace Domain.ValueObject;

/// <summary>
/// Represents a tip title with validation constraints.
/// </summary>
public sealed record TipTitle
{
    /// <summary>
    /// The minimum allowed length for a tip title.
    /// </summary>
    public const int MinLength = 5;

    /// <summary>
    /// The maximum allowed length for a tip title.
    /// </summary>
    public const int MaxLength = 200;

    public string Value { get; }

    private TipTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tip title cannot be empty", nameof(value));
        }

        if (value.Length < MinLength)
        {
            throw new ArgumentException($"Tip title must be at least {MinLength} characters", nameof(value));
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Tip title cannot exceed {MaxLength} characters", nameof(value));
        }

        Value = value.Trim();
    }

    public static TipTitle Create(string value) => new(value);
}
