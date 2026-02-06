using Domain.ValueObject;

namespace Domain.Entities;

/// <summary>
/// Represents a user's favorite tip bookmark.
/// Each favorite is uniquely identified by the combination of UserId and TipId.
/// </summary>
public sealed class UserFavorites
{
    public UserId UserId { get; }
    public TipId TipId { get; }
    public DateTime AddedAt { get; }

    private UserFavorites(UserId userId, TipId tipId, DateTime addedAt)
    {
        UserId = userId;
        TipId = tipId;
        AddedAt = addedAt;
    }

    /// <summary>
    /// Creates a new favorite entry for a user and tip.
    /// </summary>
    /// <param name="userId">The ID of the user adding the favorite.</param>
    /// <param name="tipId">The ID of the tip being favorited.</param>
    /// <returns>A new UserFavorites instance with the current timestamp.</returns>
    public static UserFavorites Create(UserId userId, TipId tipId)
    {
        return new UserFavorites(userId, tipId, DateTime.UtcNow);
    }

    /// <summary>
    /// Factory method used by persistence layers to rehydrate a <see cref="UserFavorites"/> from
    /// stored values without coupling domain logic to any specific database technology.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tipId">The tip ID.</param>
    /// <param name="addedAt">The timestamp when the favorite was added.</param>
    /// <returns>A rehydrated UserFavorites instance.</returns>
    public static UserFavorites FromPersistence(UserId userId, TipId tipId, DateTime addedAt)
    {
        return new UserFavorites(userId, tipId, addedAt);
    }

    /// <summary>
    /// Gets the composite key for this favorite in the format "userId_tipId".
    /// This is used for Firestore document identification.
    /// </summary>
    public string GetCompositeKey() => $"{UserId.Value}_{TipId.Value}";
}
