using Application.Dtos;
using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;
using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

/// <summary>
/// Firestore implementation of favorites data store.
/// Uses composite document IDs (userId_tipId) for natural deduplication.
/// </summary>
public sealed class FirestoreFavoriteDataStore(
    FirestoreDb database,
    ICollectionNameProvider collectionNameProvider) : IFirestoreFavoriteDataStore
{
    private readonly FirestoreDb _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly ICollectionNameProvider _collectionNameProvider = collectionNameProvider ?? throw new ArgumentNullException(nameof(collectionNameProvider));

    private CollectionReference GetCollection()
    {
        var collectionName = _collectionNameProvider.GetCollectionName(FirestoreCollectionNames.Favorites);
        return _database.Collection(collectionName);
    }

    public async Task<UserFavorites?> GetByCompositeKeyAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        var documentId = FavoriteDocument.CreateDocumentId(userId, tipId);
        var documentReference = GetCollection().Document(documentId);

        var snapshot = await documentReference
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<FavoriteDocument>();
        return document?.ToEntity();
    }

    public async Task<UserFavorites> AddAsync(
        UserFavorites favorite,
        CancellationToken cancellationToken = default)
    {
        var document = FavoriteDocument.FromEntity(favorite);
        var documentId = document.GetDocumentId();

        var documentReference = GetCollection().Document(documentId);

        await documentReference
            .SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return favorite;
    }

    public async Task<bool> RemoveAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        var documentId = FavoriteDocument.CreateDocumentId(userId, tipId);
        var documentReference = GetCollection().Document(documentId);

        // Check if document exists before deleting
        var snapshot = await documentReference
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return false;
        }

        await documentReference
            .DeleteAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<(IReadOnlyList<TipId> tipIds, int totalCount)> SearchAsync(
        UserId userId,
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Get all favorites for the user
        var snapshot = await GetCollection()
            .WhereEqualTo(nameof(FavoriteDocument.UserId), userId.Value.ToString())
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var documents = snapshot.Documents
            .Select(d => d.ConvertTo<FavoriteDocument>())
            .Where(doc => doc is not null)
            .ToList();

        // Apply sorting based on AddedAt (favorites don't have other sortable fields)
        // The actual tip sorting will be done at the repository level after fetching tips
        IEnumerable<FavoriteDocument> sorted = criteria.SortDirection == SortDirection.Descending
            ? documents.OrderByDescending(f => f.AddedAt)
            : documents.OrderBy(f => f.AddedAt);

        var allTipIds = sorted
            .Select(doc => TipId.Create(Guid.Parse(doc.TipId)))
            .ToList();

        var totalCount = allTipIds.Count;

        // Apply pagination
        var skip = (criteria.PageNumber - 1) * criteria.PageSize;
        var paginatedTipIds = allTipIds
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return (paginatedTipIds, totalCount);
    }

    public async Task<bool> ExistsAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await GetByCompositeKeyAsync(userId, tipId, cancellationToken)
            .ConfigureAwait(false);

        return favorite is not null;
    }
}
