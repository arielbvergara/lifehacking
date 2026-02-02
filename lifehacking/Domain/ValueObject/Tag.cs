namespace Domain.ValueObject;

public sealed record Tag
{
    private const int MinLength = 1;
    private const int MaxLength = 50;

    public string Value { get; }

    private Tag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tag cannot be empty", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length < MinLength)
        {
            throw new ArgumentException($"Tag must be at least {MinLength} character", nameof(value));
        }

        if (trimmedValue.Length > MaxLength)
        {
            throw new ArgumentException($"Tag cannot exceed {MaxLength} characters", nameof(value));
        }

        Value = trimmedValue;
    }

    public static Tag Create(string value) => new(value);
}
