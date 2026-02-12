namespace Application.Interfaces;

/// <summary>
/// Central place for well-known security event names so they do not appear
/// as magic strings across the application and WebAPI layers.
/// </summary>
public static class SecurityEventNames
{
    public const string UserCreated = "user.created";
    public const string UserCreateFailed = "user.create.failed";
    public const string UserUpdated = "user.updated";
    public const string UserUpdateFailed = "user.update.failed";
    public const string UserDeleted = "user.deleted";
    public const string UserDeleteFailed = "user.delete.failed";

    /// <summary>
    /// Emitted when an authenticated non-admin principal attempts to access an
    /// endpoint that is restricted to administrators.
    /// </summary>
    public const string AdminEndpointAccessDenied = "admin.endpoint.access.denied";

    public const string FavoriteAdded = "favorite.added";
    public const string FavoriteAddFailed = "favorite.add.failed";
    public const string FavoriteRemoved = "favorite.removed";
    public const string FavoriteRemoveFailed = "favorite.remove.failed";
    public const string FavoritesMerged = "favorites.merged";
    public const string FavoritesMergeFailed = "favorites.merge.failed";

    public const string CategoryCreated = "category.created";
    public const string CategoryCreateFailed = "category.create.failed";
    public const string CategoryUpdated = "category.updated";
    public const string CategoryUpdateFailed = "category.update.failed";
    public const string CategoryDeleted = "category.deleted";
    public const string CategoryDeleteFailed = "category.delete.failed";
    public const string CategoryImageUploadSuccess = "category.image.upload.success";
    public const string CategoryImageUploadFailed = "category.image.upload.failed";

    public const string TipCreated = "tip.created";
    public const string TipCreateFailed = "tip.create.failed";
    public const string TipUpdated = "tip.updated";
    public const string TipUpdateFailed = "tip.update.failed";
    public const string TipDeleted = "tip.deleted";
    public const string TipDeleteFailed = "tip.delete.failed";
    public const string TipImageUploadSuccess = "tip.image.upload.success";
    public const string TipImageUploadFailed = "tip.image.upload.failed";
}

/// <summary>
/// Centralizes allowed outcome values for security events.
/// </summary>
public static class SecurityEventOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
}

/// <summary>
/// Abstraction for publishing security-relevant events such as user lifecycle
/// changes or authorization failures to logging and alerting pipelines.
/// </summary>
public interface ISecurityEventNotifier
{
    /// <summary>
    /// Notify observers about a security-relevant event. Implementations are
    /// expected to be non-throwing and to avoid leaking sensitive data.
    /// </summary>
    Task NotifyAsync(
        string eventName,
        string? subjectId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null,
        CancellationToken cancellationToken = default);
}
