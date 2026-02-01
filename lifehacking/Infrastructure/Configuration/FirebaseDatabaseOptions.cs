namespace Infrastructure.Configuration;

/// <summary>
/// Options for configuring Firebase as the primary application datastore.
///
/// These options are bound from the <c>Firebase</c> configuration section and are
/// intended for use by infrastructure components that connect to the Firebase
/// database (e.g., Firestore or Realtime Database).
/// </summary>
public sealed class FirebaseDatabaseOptions
{
    /// <summary>
    /// Configuration section name for Firebase database options.
    /// </summary>
    public const string SectionName = "Firebase";

    /// <summary>
    /// Firebase project identifier used when creating database clients.
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Optional database URL or endpoint for the Firebase database.
    /// This can be used by specific client libraries when required.
    /// </summary>
    public string DatabaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Optional emulator host/port to be used in development and testing
    /// instead of the production database endpoint.
    /// </summary>
    public string EmulatorHost { get; init; } = string.Empty;

    /// <summary>
    /// Name of the users collection used for persisting <see cref="Domain.Entities.User"/>
    /// documents in Firebase. This is configurable to allow different collection
    /// names per environment while avoiding magic strings in repository code.
    /// </summary>
    public string UsersCollectionName { get; init; } = "users";
}
