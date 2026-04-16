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

public sealed class FavoritesRepository(LifehackingDbContext db) : IFavoritesRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UserFavorites?> GetByUserAndTipAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.UserFavorites
            .Where(uf => uf.UserId == userId.Value && uf.TipId == tipId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : UserFavorites.FromPersistence(
            UserId.Create(row.UserId),
            TipId.Create(row.TipId),
            row.AddedAt);
    }

    public async Task<UserFavorites> AddAsync(
        UserFavorites favorite,
        CancellationToken cancellationToken = default)
    {
        var row = new UserFavoriteRow
        {
            UserId = favorite.UserId.Value,
            TipId = favorite.TipId.Value,
            AddedAt = favorite.AddedAt
        };

        db.UserFavorites.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task<bool> RemoveAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await db.UserFavorites
            .Where(uf => uf.UserId == userId.Value && uf.TipId == tipId.Value)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task<(IReadOnlyList<Tip> tips, int totalCount)> SearchUserFavoritesAsync(
        UserId userId,
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Join user_favorites → tips; project to TipRow for further filtering/sorting
        var tipQuery = db.UserFavorites
            .Where(uf => uf.UserId == userId.Value)
            .Join(
                db.Tips.Where(t => !t.IsDeleted),
                uf => uf.TipId,
                t => t.Id,
                (_, t) => t);

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = $"%{criteria.SearchTerm.Trim()}%";
            tipQuery = tipQuery.Where(t =>
                EF.Functions.ILike(t.Title, term) ||
                EF.Functions.ILike(t.Description, term) ||
                (t.StepsSearch != null && EF.Functions.ILike(t.StepsSearch, term)));
        }

        if (criteria.CategoryId.HasValue)
        {
            var categoryId = criteria.CategoryId.Value;
            tipQuery = tipQuery.Where(t => t.CategoryId == categoryId);
        }

        if (criteria.Tags is { Count: > 0 })
        {
            foreach (var tag in criteria.Tags)
            {
                var localTag = tag;
                tipQuery = tipQuery.Where(t => t.Tags.Contains(localTag));
            }
        }

        tipQuery = ApplyOrdering(tipQuery, criteria);

        var totalCount = await tipQuery.CountAsync(cancellationToken);

        var rows = await tipQuery
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapTipToDomain).ToList(), totalCount);
    }

    public async Task<bool> ExistsAsync(
        UserId userId,
        TipId tipId,
        CancellationToken cancellationToken = default)
    {
        return await db.UserFavorites
            .AnyAsync(uf => uf.UserId == userId.Value && uf.TipId == tipId.Value, cancellationToken);
    }

    public async Task<IReadOnlySet<TipId>> GetExistingFavoritesAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        if (tipIds.Count == 0)
        {
            return new HashSet<TipId>();
        }

        var guidIds = tipIds.Select(id => id.Value).ToArray();
        var existing = await db.UserFavorites
            .Where(uf => uf.UserId == userId.Value && guidIds.Contains(uf.TipId))
            .Select(uf => uf.TipId)
            .ToListAsync(cancellationToken);

        return existing.Select(TipId.Create).ToHashSet();
    }

    public async Task<IReadOnlyList<UserFavorites>> AddBatchAsync(
        UserId userId,
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = tipIds.Select(tipId => new UserFavoriteRow
        {
            UserId = userId.Value,
            TipId = tipId.Value,
            AddedAt = now
        }).ToList();

        db.UserFavorites.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);

        return rows.Select(r => UserFavorites.FromPersistence(
            UserId.Create(r.UserId),
            TipId.Create(r.TipId),
            r.AddedAt)).ToList();
    }

    public async Task<int> RemoveAllByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await db.UserFavorites
            .Where(uf => uf.UserId == userId.Value)
            .ExecuteDeleteAsync(cancellationToken);
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

    private static Tip MapTipToDomain(TipRow row)
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
