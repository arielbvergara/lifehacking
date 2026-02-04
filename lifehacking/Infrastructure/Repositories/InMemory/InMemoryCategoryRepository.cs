using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.InMemory;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryCategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        return await context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        return await context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Name == trimmedName, cancellationToken);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await context.Set<Category>().AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        context.Set<Category>().Update(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var category = await GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return;
        }

        context.Set<Category>().Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }
}
