namespace Infrastructure.Data.Firestore;

/// <summary>
/// Collection name provider for test environments that generates unique collection names
/// by appending a GUID-based suffix to ensure test isolation.
/// Each instance generates a unique 8-character identifier in the constructor,
/// ensuring that concurrent tests use distinct collections.
/// </summary>
public sealed class TestCollectionNameProvider : ICollectionNameProvider
{
    private readonly string _uniqueIdentifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCollectionNameProvider"/> class.
    /// Generates a unique 8-character GUID-based suffix for this test instance.
    /// </summary>
    public TestCollectionNameProvider()
    {
        // Generate a unique 8-character identifier using GUID
        // This ensures uniqueness across concurrent test executions
        _uniqueIdentifier = Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Gets the collection name with a unique test-specific suffix.
    /// </summary>
    /// <param name="baseCollectionName">The base collection name (e.g., "users", "tips", "categories").</param>
    /// <returns>The collection name in the format "{baseCollectionName}_{uniqueIdentifier}".</returns>
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

        return $"{baseCollectionName}_{_uniqueIdentifier}";
    }
}
