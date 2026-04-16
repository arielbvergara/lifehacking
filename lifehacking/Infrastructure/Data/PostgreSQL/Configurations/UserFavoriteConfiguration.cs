using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.PostgreSQL.Configurations;

public sealed class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavoriteRow>
{
    public void Configure(EntityTypeBuilder<UserFavoriteRow> builder)
    {
        builder.ToTable("user_favorites");
        builder.HasKey(uf => new { uf.UserId, uf.TipId });

        builder.Property(uf => uf.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(uf => uf.TipId).HasColumnName("tip_id").IsRequired();
        builder.Property(uf => uf.AddedAt).HasColumnName("added_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(uf => uf.UserId)
            .HasDatabaseName("ix_user_favorites_user_id");

        builder.HasIndex(uf => uf.TipId)
            .HasDatabaseName("ix_user_favorites_tip_id");
    }
}
