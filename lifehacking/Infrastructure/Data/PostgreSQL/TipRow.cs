namespace Infrastructure.Data.PostgreSQL;

public sealed class TipRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tip steps serialized as a JSONB array in the database.
    /// </summary>
    public string StepsJson { get; set; } = "[]";

    /// <summary>
    /// Computed column (steps_json::text) used for full-text search within step descriptions.
    /// </summary>
    public string? StepsSearch { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>
    /// Tags stored as a PostgreSQL text[] array.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    public string? VideoUrl { get; set; }

    // ImageMetadata flattened columns (all nullable — image is optional)
    public string? ImageUrl { get; set; }
    public string? ImageStoragePath { get; set; }
    public string? ImageOriginalFileName { get; set; }
    public string? ImageContentType { get; set; }
    public long? ImageFileSizeBytes { get; set; }
    public DateTime? ImageUploadedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
