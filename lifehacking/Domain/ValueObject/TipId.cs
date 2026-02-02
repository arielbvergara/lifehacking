namespace Domain.ValueObject;

public sealed record TipId
{
    public Guid Value { get; }

    private TipId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Tip ID cannot be empty", nameof(value));
        }

        Value = value;
    }

    public static TipId Create(Guid value) => new(value);
    public static TipId NewId() => new(Guid.NewGuid());
}
