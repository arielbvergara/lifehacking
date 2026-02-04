using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Repositories;

public sealed class TipRepository(IFirestoreTipDataStore dataStore) : ITipRepository
{
    private readonly IFirestoreTipDataStore _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));

    public Task<Tip?> GetByIdAsync(TipId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByIdAsync(id, cancellationToken);
    }

    public Task<(IReadOnlyCollection<Tip> Items, int TotalCount)> SearchAsync(
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.SearchAsync(criteria, cancellationToken);
    }

    public Task<IReadOnlyCollection<Tip>> GetByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetByCategoryAsync(categoryId, cancellationToken);
    }

    public Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        return _dataStore.AddAsync(tip, cancellationToken);
    }

    public Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
    {
        return _dataStore.UpdateAsync(tip, cancellationToken);
    }

    public Task DeleteAsync(TipId id, CancellationToken cancellationToken = default)
    {
        return _dataStore.DeleteAsync(id, cancellationToken);
    }
}
