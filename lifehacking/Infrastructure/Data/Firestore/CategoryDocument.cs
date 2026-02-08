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
}
