using Domain.Entities;
using Domain.ValueObject;
using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

public sealed class FirestoreCategoryDataStore(FirestoreDb database) : IFirestoreCategoryDataStore
{
    private readonly FirestoreDb _database = database ?? throw new ArgumentNullException(nameof(database));

    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return document is null ? null : MapToDomainCategory(document);
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _database.Collection(FirestoreCollectionNames.Categories)
            .OrderBy(nameof(CategoryDocument.Name))
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var categories = snapshot.Documents
            .Select(d => d.ConvertTo<CategoryDocument>())
            .Where(doc => doc is not null)
            .Select(MapToDomainCategory)
            .ToList();

        return categories;
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var snapshot = await _database.Collection(FirestoreCollectionNames.Categories)
            .WhereEqualTo(nameof(CategoryDocument.Name), name.Trim())
            .Limit(1)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var document = snapshot.Documents
            .Select(d => d.ConvertTo<CategoryDocument>())
            .Where(doc => doc is not null)
            .FirstOrDefault();

        return document is null ? null : MapToDomainCategory(document);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(category);

        var documentReference = _database
            .Collection(FirestoreCollectionNames.Categories)
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(category);

        var documentReference = _database
            .Collection(FirestoreCollectionNames.Categories)
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var documentReference = _database
            .Collection(FirestoreCollectionNames.Categories)
            .Document(id.Value.ToString());

        await documentReference.DeleteAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<CategoryDocument?> GetDocumentByIdAsync(CategoryId id, CancellationToken cancellationToken)
    {
        var documentReference = _database
            .Collection(FirestoreCollectionNames.Categories)
            .Document(id.Value.ToString());

        var snapshot = await documentReference.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<CategoryDocument>();
        return document ?? null;
    }

    private static Category MapToDomainCategory(CategoryDocument document)
    {
        var id = CategoryId.Create(Guid.Parse(document.Id));

        return Category.FromPersistence(
            id,
            document.Name,
            document.CreatedAt,
            document.UpdatedAt);
    }

    private static CategoryDocument MapToDocument(Category category)
    {
        return new CategoryDocument
        {
            Id = category.Id.Value.ToString(),
            Name = category.Name,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
