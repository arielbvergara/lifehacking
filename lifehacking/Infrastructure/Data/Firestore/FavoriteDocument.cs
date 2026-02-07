using Domain.Entities;
using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

/// <summary>
/// Firestore document representation of a user's favorite tip.
/// Uses composite document ID format: {userId}_{tipId}
/// </summary>
[FirestoreData]
public class FavoriteDocument
{
    /// <summary>
    /// The ID of the user who favorited the tip.
    /// </summary>
    [FirestoreProperty("userId")]
    public required string UserId { get; set; }

    /// <summary>
    /// The ID of the favorited tip.
    /// </summary>
    [FirestoreProperty("tipId")]
    public required string TipId { get; set; }

    /// <summary>
    /// The timestamp when the tip was added to favorites.
    /// </summary>
    [FirestoreProperty("addedAt")]
    public required DateTime AddedAt { get; set; }

    /// <summary>
    /// Converts this Firestore document to a domain entity.
    /// </summary>
    public UserFavorites ToEntity()
    {
        var userIdValue = Domain.ValueObject.UserId.Create(Guid.Parse(UserId));
        var tipIdValue = Domain.ValueObject.TipId.Create(Guid.Parse(TipId));

        return UserFavorites.FromPersistence(userIdValue, tipIdValue, AddedAt);
    }

    /// <summary>
    /// Creates a Firestore document from a domain entity.
    /// </summary>
    public static FavoriteDocument FromEntity(UserFavorites favorite)
    {
        return new FavoriteDocument
        {
            UserId = favorite.UserId.Value.ToString(),
            TipId = favorite.TipId.Value.ToString(),
            AddedAt = favorite.AddedAt
        };
    }

    /// <summary>
    /// Gets the composite document ID for this favorite.
    /// </summary>
    public string GetDocumentId() => $"{UserId}_{TipId}";

    /// <summary>
    /// Creates a composite document ID from user and tip IDs.
    /// </summary>
    public static string CreateDocumentId(Domain.ValueObject.UserId userId, Domain.ValueObject.TipId tipId) =>
        $"{userId.Value}_{tipId.Value}";
}
