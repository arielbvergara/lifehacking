using Domain.Entities;
using Domain.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value));

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(u => u.Name)
            .HasConversion(
                name => name.Value,
                value => UserName.Create(value));

        builder.Property(u => u.ExternalAuthId)
            .HasConversion(
                externalId => externalId.Value,
                value => ExternalAuthIdentifier.Create(value));

        // Simple scalar properties for role and soft-delete state.
        builder.Property(u => u.Role)
            .HasMaxLength(100);

        builder.Property(u => u.IsDeleted);

        builder.Property(u => u.DeletedAt);
    }
}
