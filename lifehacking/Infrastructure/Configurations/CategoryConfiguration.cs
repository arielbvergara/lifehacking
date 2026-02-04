using Domain.Entities;
using Domain.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => CategoryId.Create(value))
            .HasColumnName("Id");

        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .HasMaxLength(Category.MaxNameLength)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Unique constraint on Name
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name_Unique");

        // Index for performance
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Categories_CreatedAt");
    }
}
