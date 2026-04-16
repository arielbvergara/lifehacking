using System.Text.Json;
using Application.Dtos;
using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data;
using Infrastructure.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TipRepository(LifehackingDbContext db) : ITipRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Tip?> GetByIdAsync(TipId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Tips
            .Where(t => t.Id == id.Value && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<(IReadOnlyCollection<Tip> Items, int TotalCount)> SearchAsync(
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tips.Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = $"%{criteria.SearchTerm.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Title, term) ||
                EF.Functions.ILike(t.Description, term) ||
                (t.StepsSearch != null && EF.Functions.ILike(t.StepsSearch, term)));
        }

        if (criteria.CategoryId.HasValue)
        {
            var categoryId = criteria.CategoryId.Value;
            query = query.Where(t => t.CategoryId == categoryId);
        }

        if (criteria.Tags is { Count: > 0 })
        {
            foreach (var tag in criteria.Tags)
            {
                var localTag = tag;
                query = query.Where(t => t.Tags.Contains(localTag));
            }
        }

        query = ApplyOrdering(query, criteria);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapToDomain).ToList(), totalCount);
    }

    public async Task<IReadOnlyCollection<Tip>> GetByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Tips
            .Where(t => t.CategoryId == categoryId.Value && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<int> CountByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        return await db.Tips
            .CountAsync(t => t.CategoryId == categoryId.Value && !t.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Tip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Tips
            .Where(t => !t.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        var row = MapToRow(tip);
        db.Tips.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return tip;
    }

    public async Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        var row = await db.Tips
            .Where(t => t.Id == tip.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return;
        }

        row.Title = tip.Title.Value;
        row.Description = tip.Description.Value;
        row.StepsJson = SerializeSteps(tip.Steps);
        row.CategoryId = tip.CategoryId.Value;
        row.Tags = tip.Tags.Select(t => t.Value).ToArray();
        row.VideoUrl = tip.VideoUrl?.Value;
        row.ImageUrl = tip.Image?.ImageUrl;
        row.ImageStoragePath = tip.Image?.ImageStoragePath;
        row.ImageOriginalFileName = tip.Image?.OriginalFileName;
        row.ImageContentType = tip.Image?.ContentType;
        row.ImageFileSizeBytes = tip.Image?.FileSizeBytes;
        row.ImageUploadedAt = tip.Image?.UploadedAt;
        row.UpdatedAt = tip.UpdatedAt;
        row.IsDeleted = tip.IsDeleted;
        row.DeletedAt = tip.DeletedAt;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TipId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Tips
            .Where(t => t.Id == id.Value && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return;
        }

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<TipId, Tip>> GetByIdsAsync(
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        if (tipIds.Count == 0)
        {
            return new Dictionary<TipId, Tip>();
        }

        var guidIds = tipIds.Select(id => id.Value).ToArray();
        var rows = await db.Tips
            .Where(t => guidIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => TipId.Create(row.Id),
            row => MapToDomain(row));
    }

    private static IQueryable<TipRow> ApplyOrdering(IQueryable<TipRow> query, TipQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (TipSortField.Title, SortDirection.Ascending) =>
                query.OrderBy(t => t.Title).ThenBy(t => t.Id),
            (TipSortField.Title, SortDirection.Descending) =>
                query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),
            (TipSortField.UpdatedAt, SortDirection.Ascending) =>
                query.OrderBy(t => t.UpdatedAt ?? t.CreatedAt).ThenBy(t => t.Id),
            (TipSortField.UpdatedAt, SortDirection.Descending) =>
                query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ThenByDescending(t => t.Id),
            (TipSortField.CreatedAt, SortDirection.Descending) =>
                query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id),
            _ =>
                query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
        };
    }

    private static Tip MapToDomain(TipRow row)
    {
        var steps = DeserializeSteps(row.StepsJson);
        var tags = row.Tags.Select(Tag.Create).ToList();

        var videoUrl = string.IsNullOrWhiteSpace(row.VideoUrl)
            ? null
            : VideoUrl.Create(row.VideoUrl);

        ImageMetadata? image = null;
        if (!string.IsNullOrEmpty(row.ImageUrl) &&
            !string.IsNullOrEmpty(row.ImageStoragePath) &&
            !string.IsNullOrEmpty(row.ImageOriginalFileName) &&
            !string.IsNullOrEmpty(row.ImageContentType) &&
            row.ImageFileSizeBytes.HasValue &&
            row.ImageUploadedAt.HasValue)
        {
            image = ImageMetadata.Create(
                row.ImageUrl,
                row.ImageStoragePath,
                row.ImageOriginalFileName,
                row.ImageContentType,
                row.ImageFileSizeBytes.Value,
                row.ImageUploadedAt.Value);
        }

        return Tip.FromPersistence(
            TipId.Create(row.Id),
            TipTitle.Create(row.Title),
            TipDescription.Create(row.Description),
            steps,
            CategoryId.Create(row.CategoryId),
            tags,
            videoUrl,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt,
            image);
    }

    private static TipRow MapToRow(Tip tip)
    {
        return new TipRow
        {
            Id = tip.Id.Value,
            Title = tip.Title.Value,
            Description = tip.Description.Value,
            StepsJson = SerializeSteps(tip.Steps),
            CategoryId = tip.CategoryId.Value,
            Tags = tip.Tags.Select(t => t.Value).ToArray(),
            VideoUrl = tip.VideoUrl?.Value,
            ImageUrl = tip.Image?.ImageUrl,
            ImageStoragePath = tip.Image?.ImageStoragePath,
            ImageOriginalFileName = tip.Image?.OriginalFileName,
            ImageContentType = tip.Image?.ContentType,
            ImageFileSizeBytes = tip.Image?.FileSizeBytes,
            ImageUploadedAt = tip.Image?.UploadedAt,
            CreatedAt = tip.CreatedAt,
            UpdatedAt = tip.UpdatedAt,
            IsDeleted = tip.IsDeleted,
            DeletedAt = tip.DeletedAt
        };
    }

    private static string SerializeSteps(IReadOnlyList<TipStep> steps)
    {
        var rows = steps.Select(s => new TipStepRow { StepNumber = s.StepNumber, Description = s.Description });
        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private static List<TipStep> DeserializeSteps(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == "[]")
        {
            return new List<TipStep>();
        }

        var rows = JsonSerializer.Deserialize<List<TipStepRow>>(stepsJson, JsonOptions)
                   ?? new List<TipStepRow>();

        return rows.Select(r => TipStep.Create(r.StepNumber, r.Description)).ToList();
    }
}
