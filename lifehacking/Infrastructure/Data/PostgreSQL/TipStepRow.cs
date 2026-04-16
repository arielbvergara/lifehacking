namespace Infrastructure.Data.PostgreSQL;

/// <summary>
/// Represents a single tip step as serialized in the JSONB steps column.
/// </summary>
public sealed class TipStepRow
{
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
}
