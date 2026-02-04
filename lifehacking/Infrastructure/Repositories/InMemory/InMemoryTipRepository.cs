using Application.Dtos;
using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.InMemory;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryTipRepository(AppDbContext context) : ITipRepository
{
    public async Task<Tip?> GetByIdAsync(TipId id, CancellationToken cancellationToken = default)
    {
        return await context.Set<Tip>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Tip> Items, int TotalCount)> SearchAsync(
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // Load all tips from the database and apply search, ordering, and
        // pagination in memory. This keeps the repository simple and avoids complex
        // provider-specific translation issues for value objects.
        var tips = await context.Set<Tip>()
            .ToListAsync(cancellationToken);

        IEnumerable<Tip> filtered = tips;

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.Trim();
            filtered = filtered.Where(tip =>
                tip.Title.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                tip.Description.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                tip.Steps.Any(step => step.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                tip.Tags.Any(tag => tag.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply category filter
        if (criteria.CategoryId.HasValue)
        {
            var categoryId = CategoryId.Create(criteria.CategoryId.Value);
            filtered = filtered.Where(tip => tip.CategoryId == categoryId);
        }

        // Apply tags filter
        if (criteria.Tags is not null && criteria.Tags.Count > 0)
        {
            filtered = filtered.Where(tip =>
                criteria.Tags.All(tag => tip.Tags.Any(t => t.Value.Equals(tag, StringComparison.OrdinalIgnoreCase))));
        }

        filtered = ApplyOrdering(filtered, criteria);

        var totalCount = filtered.Count();

        var skip = (criteria.PageNumber - 1) * criteria.PageSize;

        var items = filtered
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<Tip>> GetByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<Tip>()
            .Where(t => t.CategoryId == categoryId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        await context.Set<Tip>().AddAsync(tip, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return tip;
    }

    public async Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        context.Set<Tip>().Update(tip);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TipId id, CancellationToken cancellationToken = default)
    {
        var tip = await GetByIdAsync(id, cancellationToken);
        if (tip is null)
        {
            return;
        }

        context.Set<Tip>().Remove(tip);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Tip> ApplyOrdering(IEnumerable<Tip> query, TipQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (TipSortField.Title, SortDirection.Ascending) =>
                query.OrderBy(tip => tip.Title.Value)
                     .ThenBy(tip => tip.Id.Value),
            (TipSortField.Title, SortDirection.Descending) =>
                query.OrderByDescending(tip => tip.Title.Value)
                     .ThenByDescending(tip => tip.Id.Value),
            (TipSortField.UpdatedAt, SortDirection.Ascending) =>
                query.OrderBy(tip => tip.UpdatedAt ?? tip.CreatedAt)
                     .ThenBy(tip => tip.Id.Value),
            (TipSortField.UpdatedAt, SortDirection.Descending) =>
                query.OrderByDescending(tip => tip.UpdatedAt ?? tip.CreatedAt)
                     .ThenByDescending(tip => tip.Id.Value),
            (TipSortField.CreatedAt, SortDirection.Descending) =>
                query.OrderByDescending(tip => tip.CreatedAt)
                     .ThenByDescending(tip => tip.Id.Value),
            _ =>
                query.OrderBy(tip => tip.CreatedAt)
                     .ThenBy(tip => tip.Id.Value)
        };
    }
}
