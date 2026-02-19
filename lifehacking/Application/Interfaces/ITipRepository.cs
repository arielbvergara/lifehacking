using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;

namespace Application.Interfaces;

public interface ITipRepository
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

    /// <summary>
    /// Gets tips by their IDs in a batch operation.
    /// Returns only the tips that exist and are not soft-deleted.
    /// </summary>
    /// <param name="tipIds">The collection of tip IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping tip IDs to their corresponding tip entities.</returns>
    Task<IReadOnlyDictionary<TipId, Tip>> GetByIdsAsync(
        IReadOnlyCollection<TipId> tipIds,
        CancellationToken cancellationToken = default);
}
