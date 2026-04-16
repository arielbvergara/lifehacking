namespace Infrastructure.Data.PostgreSQL;

public sealed class UserFavoriteRow
{
    public Guid UserId { get; set; }
    public Guid TipId { get; set; }
    public DateTime AddedAt { get; set; }
}
