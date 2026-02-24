using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Repositories;

public sealed class CategoryRepository(IFirestoreCategoryDataStore dataStore) : ICategoryRepository
{
    private readonly IFirestoreCategoryDataStore _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));

    public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyDictionary<CategoryId, Category>> GetByIdsAsync(
        IReadOnlyCollection<CategoryId> ids,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByIdsAsync(ids, cancellationToken);
    }

    public Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dataStore.GetAllAsync(cancellationToken);
    }

    public Task<Category?> GetByNameAsync(string name, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByNameAsync(name, includeDeleted, cancellationToken);
    }

    public Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _dataStore.AddAsync(category, cancellationToken);
    }

    public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _dataStore.UpdateAsync(category, cancellationToken);
    }

    public Task DeleteAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.DeleteAsync(id, cancellationToken);
    }
}
