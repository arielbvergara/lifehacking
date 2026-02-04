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

    [FirestoreProperty("youtubeUrl")]
    public string? YouTubeUrl { get; set; }

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

[FirestoreData]
public sealed class TipStepDocument
{
    [FirestoreProperty("stepNumber")]
    public int StepNumber { get; set; }

    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;
}
