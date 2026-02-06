namespace Infrastructure.Data.Firestore;

/// <summary>
/// Collection name provider for production environments that returns base collection names unchanged.
/// This provider does not apply any transformations or suffixes to collection names.
/// </summary>
public sealed class ProductionCollectionNameProvider : ICollectionNameProvider
{
    /// <summary>
    /// Gets the collection name unchanged from the base collection name.
    /// </summary>
    /// <param name="baseCollectionName">The base collection name (e.g., "users", "tips", "categories").</param>
    /// <returns>The base collection name without any modifications.</returns>
    /// <exception cref="ArgumentNullException">Thrown when baseCollectionName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when baseCollectionName is empty or whitespace.</exception>
    public string GetCollectionName(string baseCollectionName)
    {
        if (baseCollectionName == null)
        {
            throw new ArgumentNullException(nameof(baseCollectionName));
        }

        if (string.IsNullOrWhiteSpace(baseCollectionName))
        {
            throw new ArgumentException("Base collection name cannot be empty or whitespace.", nameof(baseCollectionName));
        }

        return baseCollectionName;
    }
}
