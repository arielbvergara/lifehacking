using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;

namespace Infrastructure.Data.Firestore;

public interface IFirestoreTipDataStore
{
    Task<Tip?> GetByIdAsync(TipId id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Tip> Items, int TotalCount)> SearchAsync(
        TipQueryCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Tip>> GetByCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Tip>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default);

    Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default);

    Task DeleteAsync(TipId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Tip>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TipId, Tip>> GetByIdsAsync(
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default);
}
