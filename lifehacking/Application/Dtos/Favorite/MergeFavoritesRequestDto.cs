using System.Text.Json.Serialization;

namespace Application.Dtos.Favorite;

/// <summary>
/// Web API DTO for merging anonymous favorites into server-side favorites.
/// </summary>
/// <param name="TipIds">The list of tip IDs from client local storage to merge.</param>
public record MergeFavoritesRequestDto(
    [property: JsonPropertyName("tipIds")] List<Guid> TipIds
);
