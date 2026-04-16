using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.PostgreSQL.Configurations;

public sealed class TipConfiguration : IEntityTypeConfiguration<TipRow>
{
    public void Configure(EntityTypeBuilder<TipRow> builder)
    {
        builder.ToTable("tips");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").IsRequired();
        builder.Property(t => t.CategoryId).HasColumnName("category_id").IsRequired();

        // Steps stored as JSONB; StepsSearch is a PostgreSQL generated column (steps_json::text)
        // for efficient full-text search over step descriptions.
        builder.Property(t => t.StepsJson)
            .HasColumnName("steps_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(t => t.StepsSearch)
            .HasColumnName("steps_search")
            .HasComputedColumnSql("steps_json::text", stored: true);

        // Tags stored as a PostgreSQL text[] array
        builder.Property(t => t.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(t => t.VideoUrl).HasColumnName("video_url").HasMaxLength(2048);

        // Flattened ImageMetadata columns
        builder.Property(t => t.ImageUrl).HasColumnName("image_url").HasMaxLength(2048);
        builder.Property(t => t.ImageStoragePath).HasColumnName("image_storage_path").HasMaxLength(1024);
        builder.Property(t => t.ImageOriginalFileName).HasColumnName("image_original_file_name").HasMaxLength(255);
        builder.Property(t => t.ImageContentType).HasColumnName("image_content_type").HasMaxLength(100);
        builder.Property(t => t.ImageFileSizeBytes).HasColumnName("image_file_size_bytes");
        builder.Property(t => t.ImageUploadedAt).HasColumnName("image_uploaded_at").HasColumnType("timestamptz");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.CategoryId)
            .HasFilter("is_deleted = FALSE")
            .HasDatabaseName("ix_tips_category_id_active");

        builder.HasIndex(t => t.IsDeleted)
            .HasDatabaseName("ix_tips_is_deleted");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_tips_created_at");
    }
}
