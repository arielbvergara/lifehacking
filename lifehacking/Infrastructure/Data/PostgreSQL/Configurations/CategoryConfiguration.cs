using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.PostgreSQL.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryRow>
{
    public void Configure(EntityTypeBuilder<CategoryRow> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // Flattened ImageMetadata columns
        builder.Property(c => c.ImageUrl).HasColumnName("image_url").HasMaxLength(2048);
        builder.Property(c => c.ImageStoragePath).HasColumnName("image_storage_path").HasMaxLength(1024);
        builder.Property(c => c.ImageOriginalFileName).HasColumnName("image_original_file_name").HasMaxLength(255);
        builder.Property(c => c.ImageContentType).HasColumnName("image_content_type").HasMaxLength(100);
        builder.Property(c => c.ImageFileSizeBytes).HasColumnName("image_file_size_bytes");
        builder.Property(c => c.ImageUploadedAt).HasColumnName("image_uploaded_at").HasColumnType("timestamptz");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("ix_categories_is_deleted");
    }
}
