using Domain.Entities;
using Domain.ValueObject;

namespace Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets categories by their IDs in a batch operation.
    /// Returns only the categories that exist and are not soft-deleted.
    /// </summary>
    /// <param name="ids">The collection of category IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping category IDs to their corresponding category entities.</returns>
    Task<IReadOnlyDictionary<CategoryId, Category>> GetByIdsAsync(
        IReadOnlyCollection<CategoryId> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default);
}
