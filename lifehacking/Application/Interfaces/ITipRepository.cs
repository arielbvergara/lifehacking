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

    Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default);

    Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default);

    Task DeleteAsync(TipId id, CancellationToken cancellationToken = default);
}
