using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.PostgreSQL.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserRow>
{
    public void Configure(EntityTypeBuilder<UserRow> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.ExternalAuthId).HasColumnName("external_auth_id").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(u => u.Email)
            .HasFilter("is_deleted = FALSE")
            .IsUnique()
            .HasDatabaseName("ix_users_email_active");

        builder.HasIndex(u => u.ExternalAuthId)
            .HasFilter("is_deleted = FALSE")
            .IsUnique()
            .HasDatabaseName("ix_users_external_auth_active");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("ix_users_created_at");

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("ix_users_is_deleted");
    }
}
