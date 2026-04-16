namespace Infrastructure.Data.PostgreSQL;

public sealed class CategoryRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ImageMetadata flattened columns (all nullable — image is optional)
    public string? ImageUrl { get; set; }
    public string? ImageStoragePath { get; set; }
    public string? ImageOriginalFileName { get; set; }
    public string? ImageContentType { get; set; }
    public long? ImageFileSizeBytes { get; set; }
    public DateTime? ImageUploadedAt { get; set; }
}
