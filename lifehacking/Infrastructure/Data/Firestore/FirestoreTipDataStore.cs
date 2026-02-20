using Application.Dtos;
using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;
using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

public sealed class FirestoreTipDataStore(
    FirestoreDb database,
    ICollectionNameProvider collectionNameProvider) : IFirestoreTipDataStore
{
    private readonly FirestoreDb _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly ICollectionNameProvider _collectionNameProvider = collectionNameProvider ?? throw new ArgumentNullException(nameof(collectionNameProvider));

    private CollectionReference GetCollection()
    {
        var collectionName = _collectionNameProvider.GetCollectionName(FirestoreCollectionNames.Tips);
        return _database.Collection(collectionName);
    }

    public async Task<Tip?> GetByIdAsync(TipId id, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByIdAsync(id, cancellationToken).ConfigureAwait(false);

        // Filter out soft-deleted tips
        if (document is null || document.IsDeleted)
        {
            return null;
        }

        return MapToDomainTip(document);
    }

    public async Task<(IReadOnlyCollection<Tip> Items, int TotalCount)> SearchAsync(
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCollection()
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var documents = snapshot.Documents
            .Select(d => d.ConvertTo<TipDocument>())
            .Where(doc => doc is not null)
            .ToList();

        // Filter out soft-deleted tips
        IEnumerable<TipDocument> filtered = documents.Where(doc => !doc.IsDeleted);

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.Trim();
            filtered = filtered.Where(document =>
                document.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                document.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                document.Steps.Any(step => step.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                document.Tags.Any(tag => tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply category filter
        if (criteria.CategoryId.HasValue)
        {
            var categoryIdString = criteria.CategoryId.Value.ToString();
            filtered = filtered.Where(document => document.CategoryId == categoryIdString);
        }

        // Apply tags filter
        if (criteria.Tags is not null && criteria.Tags.Count > 0)
        {
            filtered = filtered.Where(document =>
                criteria.Tags.All(tag => document.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        filtered = ApplyOrdering(filtered, criteria);

        var tips = filtered
            .Select(MapToDomainTip)
            .ToList();

        var totalCount = tips.Count;

        var skip = (criteria.PageNumber - 1) * criteria.PageSize;

        var items = tips
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<Tip>> GetByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCollection()
            .WhereEqualTo("isDeleted", false)
            .WhereEqualTo("categoryId", categoryId.Value.ToString())
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var tips = snapshot.Documents
            .Select(d => d.ConvertTo<TipDocument>())
            .Where(doc => doc is not null)
            .Select(MapToDomainTip)
            .ToList();

        return tips;
    }

    public async Task<int> CountByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCollection()
            .WhereEqualTo("isDeleted", false)
            .WhereEqualTo("categoryId", categoryId.Value.ToString())
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Count;
    }

    public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(tip);

        var documentReference = GetCollection()
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return tip;
    }

    public async Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(tip);

        var documentReference = GetCollection()
            .Document(document.Id);

        await documentReference.SetAsync(document, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(TipId id, CancellationToken cancellationToken = default)
    {
        var documentReference = GetCollection()
            .Document(id.Value.ToString());

        await documentReference.DeleteAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Tip>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCollection()
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var tips = snapshot.Documents
            .Select(d => d.ConvertTo<TipDocument>())
            .Where(doc => doc is not null)
            .Select(MapToDomainTip)
            .ToList();

        return tips;
    }

    public async Task<IReadOnlyCollection<Tip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCollection()
            .WhereEqualTo("isDeleted", false)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var tips = snapshot.Documents
            .Select(d => d.ConvertTo<TipDocument>())
            .Where(doc => doc is not null)
            .Select(MapToDomainTip)
            .ToList();

        return tips;
    }

    public async Task<IReadOnlyDictionary<TipId, Tip>> GetByIdsAsync(
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        // Handle empty input
        if (tipIds.Count == 0)
        {
            return new Dictionary<TipId, Tip>();
        }

        // Firestore WhereIn supports max 10 items per query, so batch into groups of 10
        const int batchSize = 10;
        var tipIdsList = tipIds.ToList();
        var allTips = new Dictionary<TipId, Tip>();

        for (var i = 0; i < tipIdsList.Count; i += batchSize)
        {
            var batch = tipIdsList.Skip(i).Take(batchSize).ToList();
            var tipIdStrings = batch.Select(id => id.Value.ToString()).ToList();

            var snapshot = await GetCollection()
                .WhereIn(FieldPath.DocumentId, tipIdStrings)
                .WhereEqualTo("isDeleted", false)
                .GetSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var document in snapshot.Documents)
            {
                var tipDocument = document.ConvertTo<TipDocument>();
                if (tipDocument is not null)
                {
                    var tip = MapToDomainTip(tipDocument);
                    allTips[tip.Id] = tip;
                }
            }
        }

        return allTips;
    }

    private async Task<TipDocument?> GetDocumentByIdAsync(TipId id, CancellationToken cancellationToken)
    {
        var documentReference = GetCollection()
            .Document(id.Value.ToString());

        var snapshot = await documentReference.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return null;
        }

        var document = snapshot.ConvertTo<TipDocument>();
        return document ?? null;
    }

    private static IEnumerable<TipDocument> ApplyOrdering(
        IEnumerable<TipDocument> documents,
        TipQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (TipSortField.Title, SortDirection.Ascending) =>
                documents.OrderBy(tip => tip.Title)
                    .ThenBy(tip => tip.Id),
            (TipSortField.Title, SortDirection.Descending) =>
                documents.OrderByDescending(tip => tip.Title)
                    .ThenByDescending(tip => tip.Id),
            (TipSortField.UpdatedAt, SortDirection.Ascending) =>
                documents.OrderBy(tip => tip.UpdatedAt ?? tip.CreatedAt)
                    .ThenBy(tip => tip.Id),
            (TipSortField.UpdatedAt, SortDirection.Descending) =>
                documents.OrderByDescending(tip => tip.UpdatedAt ?? tip.CreatedAt)
                    .ThenByDescending(tip => tip.Id),
            (TipSortField.CreatedAt, SortDirection.Descending) =>
                documents.OrderByDescending(tip => tip.CreatedAt)
                    .ThenByDescending(tip => tip.Id),
            _ =>
                documents.OrderBy(tip => tip.CreatedAt)
                    .ThenBy(tip => tip.Id)
        };
    }

    private static Tip MapToDomainTip(TipDocument document)
    {
        var id = TipId.Create(Guid.Parse(document.Id));
        var title = TipTitle.Create(document.Title);
        var description = TipDescription.Create(document.Description);
        var steps = document.Steps.Select(s => TipStep.Create(s.StepNumber, s.Description)).ToList();
        var categoryId = CategoryId.Create(Guid.Parse(document.CategoryId));
        var tags = document.Tags.Select(Tag.Create).ToList();
        var videoUrl = string.IsNullOrWhiteSpace(document.VideoUrl)
            ? null
            : VideoUrl.Create(document.VideoUrl);

        // Reconstruct TipImage if all required fields are present
        TipImage? image = null;
        if (!string.IsNullOrEmpty(document.ImageUrl) &&
            !string.IsNullOrEmpty(document.ImageStoragePath) &&
            !string.IsNullOrEmpty(document.OriginalFileName) &&
            !string.IsNullOrEmpty(document.ContentType) &&
            document.FileSizeBytes.HasValue &&
            document.UploadedAt.HasValue)
        {
            image = TipImage.Create(
                document.ImageUrl,
                document.ImageStoragePath,
                document.OriginalFileName,
                document.ContentType,
                document.FileSizeBytes.Value,
                document.UploadedAt.Value);
        }

        return Tip.FromPersistence(
            id,
            title,
            description,
            steps,
            categoryId,
            tags,
            videoUrl,
            document.CreatedAt,
            document.UpdatedAt,
            document.IsDeleted,
            document.DeletedAt,
            image);
    }

    private static TipDocument MapToDocument(Tip tip)
    {
        return new TipDocument
        {
            Id = tip.Id.Value.ToString(),
            Title = tip.Title.Value,
            Description = tip.Description.Value,
            Steps = tip.Steps.Select(s => new TipStepDocument
            {
                StepNumber = s.StepNumber,
                Description = s.Description
            }).ToList(),
            CategoryId = tip.CategoryId.Value.ToString(),
            Tags = tip.Tags.Select(t => t.Value).ToList(),
            VideoUrl = tip.VideoUrl?.Value,
            ImageUrl = tip.Image?.ImageUrl,
            ImageStoragePath = tip.Image?.ImageStoragePath,
            OriginalFileName = tip.Image?.OriginalFileName,
            ContentType = tip.Image?.ContentType,
            FileSizeBytes = tip.Image?.FileSizeBytes,
            UploadedAt = tip.Image?.UploadedAt,
            CreatedAt = tip.CreatedAt,
            UpdatedAt = tip.UpdatedAt,
            IsDeleted = tip.IsDeleted,
            DeletedAt = tip.DeletedAt
        };
    }
}
