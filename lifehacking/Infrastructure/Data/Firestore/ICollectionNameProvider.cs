namespace Infrastructure.Data.Firestore;

/// <summary>
/// Provides collection names for Firestore operations.
/// Implementations can apply transformations such as adding test-specific suffixes
/// for test isolation or returning base names unchanged for production use.
/// </summary>
public interface ICollectionNameProvider
{
    /// <summary>
    /// Gets the collection name to use for Firestore operations.
    /// </summary>
    /// <param name="baseCollectionName">The base collection name (e.g., "users", "tips", "categories").</param>
    /// <returns>The collection name to use, potentially with transformations applied.</returns>
    string GetCollectionName(string baseCollectionName);
}
