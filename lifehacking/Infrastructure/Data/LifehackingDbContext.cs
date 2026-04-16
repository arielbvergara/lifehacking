using Infrastructure.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class LifehackingDbContext(DbContextOptions<LifehackingDbContext> options) : DbContext(options)
{
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<CategoryRow> Categories => Set<CategoryRow>();
    public DbSet<TipRow> Tips => Set<TipRow>();
    public DbSet<UserFavoriteRow> UserFavorites => Set<UserFavoriteRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LifehackingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
