using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

[FirestoreData]
public sealed class CategoryDocument
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("name")]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty("nameLowercase")]
    public string NameLowercase { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [FirestoreProperty("isDeleted")]
    public bool IsDeleted { get; set; }

    [FirestoreProperty("deletedAt")]
    public DateTime? DeletedAt { get; set; }

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
}
