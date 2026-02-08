using Domain.Entities;
using Domain.ValueObject;

namespace Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default);
}
