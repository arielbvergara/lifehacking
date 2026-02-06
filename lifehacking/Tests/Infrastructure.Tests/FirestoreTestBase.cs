using Google.Cloud.Firestore;
using Grpc.Core;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;

namespace Infrastructure.Tests;

/// <summary>
/// Base class for tests that use Firestore emulator.
/// Provides setup and cleanup for isolated test databases.
/// </summary>
public abstract class FirestoreTestBase : IDisposable
{
    protected FirestoreDb FirestoreDb { get; private set; }
    protected ICollectionNameProvider CollectionNameProvider { get; private set; }
    protected UserRepository UserRepository { get; private set; }
    protected TipRepository TipRepository { get; private set; }
    protected CategoryRepository CategoryRepository { get; private set; }

    protected FirestoreTestBase()
    {
        var testProjectId = "demo-test"; // Use consistent fake project ID for emulator testing

        // Ensure emulator host is set BEFORE creating FirestoreDb
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8080");

        // Clear any existing credentials - emulator doesn't need them
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);

        // Create Firestore instance for emulator testing with proper configuration
        var builder = new FirestoreDbBuilder
        {
            ProjectId = testProjectId,
            Endpoint = "127.0.0.1:8080",
            ChannelCredentials = ChannelCredentials.Insecure
        };
        FirestoreDb = builder.Build();

        // Create collection name provider for test isolation
        CollectionNameProvider = new TestCollectionNameProvider();

        // Create data stores with collection name provider
        var userDataStore = new FirestoreUserDataStore(FirestoreDb, CollectionNameProvider);
        var tipDataStore = new FirestoreTipDataStore(FirestoreDb, CollectionNameProvider);
        var categoryDataStore = new FirestoreCategoryDataStore(FirestoreDb, CollectionNameProvider);

        // Create repositories
        UserRepository = new UserRepository(userDataStore);
        TipRepository = new TipRepository(tipDataStore);
        CategoryRepository = new CategoryRepository(categoryDataStore);
    }

    /// <summary>
    /// Cleans up test data by clearing all collections in the test database.
    /// </summary>
    protected async Task CleanupTestDataAsync()
    {
        try
        {
            // Get all collections and delete their documents
            var collections = new[] {
                FirestoreCollectionNames.Users,
                FirestoreCollectionNames.Tips,
                FirestoreCollectionNames.Categories
            };

            foreach (var collectionName in collections)
            {
                var collection = FirestoreDb.Collection(collectionName);
                var snapshot = await collection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    await document.Reference.DeleteAsync();
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail tests due to cleanup issues
            Console.WriteLine($"Warning: Failed to cleanup test data: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that the emulator is running and accessible.
    /// </summary>
    protected static async Task<bool> IsEmulatorRunningAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(2);
            var response = await httpClient.GetAsync("http://127.0.0.1:8080");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public virtual void Dispose()
    {
        // No cleanup needed - each test has its own collections due to collection namespacing
        GC.SuppressFinalize(this);
    }
}
