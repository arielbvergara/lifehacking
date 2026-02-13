using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

[FirestoreData]
public sealed class TipDocument
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;

    [FirestoreProperty("steps")]
    public List<TipStepDocument> Steps { get; set; } = new();

    [FirestoreProperty("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [FirestoreProperty("tags")]
    public List<string> Tags { get; set; } = new();

    [FirestoreProperty("videoUrl")]
    public string? VideoUrl { get; set; }

    [FirestoreProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [FirestoreProperty("imageStoragePath")]
    public string? ImageStoragePath { get; set; }

    [FirestoreProperty("originalFileName")]
    public string? OriginalFileName { get; set; }

    [FirestoreProperty("contentType")]
    public string? ContentType { get; set; }

    [FirestoreProperty("fileSizeBytes")]
    public long? FileSizeBytes { get; set; }

    [FirestoreProperty("uploadedAt")]
    public DateTime? UploadedAt { get; set; }

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [FirestoreProperty("isDeleted")]
    public bool IsDeleted { get; set; }

    [FirestoreProperty("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}

[FirestoreData]
public sealed class TipStepDocument
{
    [FirestoreProperty("stepNumber")]
    public int StepNumber { get; set; }

    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;
}
