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
            .WhereEqualTo("userId", userId.Value.ToString())
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

    public async Task<IReadOnlySet<TipId>> GetExistingFavoritesAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        // Handle empty input
        if (tipIds.Count == 0)
        {
            return new HashSet<TipId>();
        }

        // Firestore WhereIn supports max 10 items per query, so batch into groups of 10
        const int batchSize = 10;
        var tipIdsList = tipIds.ToList();
        var existingTipIds = new HashSet<TipId>();

        for (var i = 0; i < tipIdsList.Count; i += batchSize)
        {
            var batch = tipIdsList.Skip(i).Take(batchSize).ToList();
            var tipIdStrings = batch.Select(id => id.Value.ToString()).ToList();

            var snapshot = await GetCollection()
                .WhereEqualTo("userId", userId.Value.ToString())
                .WhereIn("tipId", tipIdStrings)
                .GetSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var document in snapshot.Documents)
            {
                var favoriteDocument = document.ConvertTo<FavoriteDocument>();
                if (favoriteDocument is not null)
                {
                    existingTipIds.Add(TipId.Create(Guid.Parse(favoriteDocument.TipId)));
                }
            }
        }

        return existingTipIds;
    }

    public async Task<IReadOnlyList<UserFavorites>> AddBatchAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        // Handle empty input
        if (tipIds.Count == 0)
        {
            return Array.Empty<UserFavorites>();
        }

        // Firestore batch write supports max 500 operations, so batch into groups of 500
        const int batchSize = 500;
        var tipIdsList = tipIds.ToList();
        var addedFavorites = new List<UserFavorites>();

        for (var i = 0; i < tipIdsList.Count; i += batchSize)
        {
            var batch = tipIdsList.Skip(i).Take(batchSize).ToList();
            var writeBatch = _database.StartBatch();

            foreach (var tipId in batch)
            {
                var favorite = UserFavorites.Create(userId, tipId);
                var document = FavoriteDocument.FromEntity(favorite);
                var documentId = document.GetDocumentId();
                var documentReference = GetCollection().Document(documentId);

                writeBatch.Set(documentReference, document);
                addedFavorites.Add(favorite);
            }

            await writeBatch.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return addedFavorites;
    }

    public async Task<int> RemoveAllByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // Query all favorites for the user
        var snapshot = await GetCollection()
            .WhereEqualTo("userId", userId.Value.ToString())
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        if (snapshot.Count == 0)
        {
            return 0;
        }

        // Firestore batch delete supports max 500 operations
        const int batchSize = 500;
        var documents = snapshot.Documents.ToList();
        var totalDeleted = 0;

        for (var i = 0; i < documents.Count; i += batchSize)
        {
            var batch = documents.Skip(i).Take(batchSize).ToList();
            var writeBatch = _database.StartBatch();

            foreach (var document in batch)
            {
                writeBatch.Delete(document.Reference);
                totalDeleted++;
            }

            await writeBatch.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return totalDeleted;
    }
}
