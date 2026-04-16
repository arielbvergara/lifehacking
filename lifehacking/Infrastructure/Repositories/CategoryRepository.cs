using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data;
using Infrastructure.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CategoryRepository(LifehackingDbContext db) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Categories
            .Where(c => c.Id == id.Value && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<IReadOnlyDictionary<CategoryId, Category>> GetByIdsAsync(
        IReadOnlyCollection<CategoryId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<CategoryId, Category>();
        }

        var guidIds = ids.Select(id => id.Value).ToArray();
        var rows = await db.Categories
            .Where(c => guidIds.Contains(c.Id) && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => CategoryId.Create(row.Id),
            row => MapToDomain(row));
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<Category?> GetByNameAsync(
        string name,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Categories.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        var row = await query
            .Where(c => EF.Functions.ILike(c.Name, name.Trim()))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        var row = MapToRow(category);
        db.Categories.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var row = await db.Categories
            .Where(c => c.Id == category.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return;
        }

        row.Name = category.Name;
        row.UpdatedAt = category.UpdatedAt;
        row.IsDeleted = category.IsDeleted;
        row.DeletedAt = category.DeletedAt;
        row.ImageUrl = category.Image?.ImageUrl;
        row.ImageStoragePath = category.Image?.ImageStoragePath;
        row.ImageOriginalFileName = category.Image?.OriginalFileName;
        row.ImageContentType = category.Image?.ContentType;
        row.ImageFileSizeBytes = category.Image?.FileSizeBytes;
        row.ImageUploadedAt = category.Image?.UploadedAt;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Categories
            .Where(c => c.Id == id.Value && !c.IsDeleted)
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

    private static Category MapToDomain(CategoryRow row)
    {
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

        return Category.FromPersistence(
            CategoryId.Create(row.Id),
            row.Name,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt,
            image);
    }

    private static CategoryRow MapToRow(Category category)
    {
        return new CategoryRow
        {
            Id = category.Id.Value,
            Name = category.Name,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            IsDeleted = category.IsDeleted,
            DeletedAt = category.DeletedAt,
            ImageUrl = category.Image?.ImageUrl,
            ImageStoragePath = category.Image?.ImageStoragePath,
            ImageOriginalFileName = category.Image?.OriginalFileName,
            ImageContentType = category.Image?.ContentType,
            ImageFileSizeBytes = category.Image?.FileSizeBytes,
            ImageUploadedAt = category.Image?.UploadedAt
        };
    }
}
