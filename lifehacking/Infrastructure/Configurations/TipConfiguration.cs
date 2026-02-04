using System.Text.Json;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TipConfiguration : IEntityTypeConfiguration<Tip>
{
    public void Configure(EntityTypeBuilder<Tip> builder)
    {
        builder.ToTable("Tips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => TipId.Create(value))
            .HasColumnName("Id");

        builder.Property(t => t.Title)
            .HasConversion(
                title => title.Value,
                value => TipTitle.Create(value))
            .HasColumnName("Title")
            .HasMaxLength(TipTitle.MaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasConversion(
                description => description.Value,
                value => TipDescription.Create(value))
            .HasColumnName("Description")
            .HasMaxLength(TipDescription.MaxLength)
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .HasConversion(
                categoryId => categoryId.Value,
                value => CategoryId.Create(value))
            .HasColumnName("CategoryId")
            .IsRequired();

        builder.Property(t => t.YouTubeUrl)
            .HasConversion(
                url => url != null ? url.Value : null,
                value => value != null ? YouTubeUrl.Create(value) : null)
            .HasColumnName("YouTubeUrl")
            .HasMaxLength(YouTubeUrl.MaxLength);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Configure Steps as JSON
        builder.Property(t => t.Steps)
            .HasConversion(
                steps => JsonSerializer.Serialize(
                    steps.Select(s => new { StepNumber = s.StepNumber, Description = s.Description }),
                    JsonSerializerOptionsDefaults.DatabaseStorage),
                json => JsonSerializer.Deserialize<List<StepData>>(json, JsonSerializerOptionsDefaults.DatabaseStorage)!
                    .Select(s => TipStep.Create(s.StepNumber, s.Description))
                    .ToList())
            .HasColumnName("Steps")
            .HasColumnType("TEXT")
            .IsRequired();

        // Configure Tags as JSON
        builder.Property(t => t.Tags)
            .HasConversion(
                tags => JsonSerializer.Serialize(tags.Select(t => t.Value), JsonSerializerOptionsDefaults.DatabaseStorage),
                json => JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptionsDefaults.DatabaseStorage)!
                    .Select(Tag.Create)
                    .ToList())
            .HasColumnName("Tags")
            .HasColumnType("TEXT")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("IX_Tips_CategoryId");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Tips_CreatedAt");

        builder.HasIndex(t => t.Title)
            .HasDatabaseName("IX_Tips_Title");
    }

    private sealed class StepData
    {
        public int StepNumber { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
