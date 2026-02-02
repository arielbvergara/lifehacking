namespace Domain.ValueObject;

public sealed record CategoryId
{
    public Guid Value { get; }

    private CategoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty", nameof(value));
        }

        Value = value;
    }

    public static CategoryId Create(Guid value) => new(value);
    public static CategoryId NewId() => new(Guid.NewGuid());
}
