using Domain.ValueObject;

namespace Application.Dtos.Favorite;

/// <summary>
/// Request to merge a list of tip IDs into the user's favorites collection.
/// </summary>
/// <param name="UserId">The ID of the user performing the merge.</param>
/// <param name="TipIds">The collection of tip IDs to merge into the user's favorites.</param>
public record MergeFavoritesRequest(
    UserId UserId,
    IReadOnlyCollection<TipId> TipIds
);
