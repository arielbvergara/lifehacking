using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Verifies that concurrent repository operations against PostgreSQL are safe and isolated
/// when each operation runs within its own DbContext instance (the standard EF Core pattern).
/// </summary>
public sealed class ParallelExecutionIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("parallel_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Create schema once via EnsureCreated
        var opts = new DbContextOptionsBuilder<LifehackingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var db = new LifehackingDbContext(opts);
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private LifehackingDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<LifehackingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new LifehackingDbContext(opts);
    }

    [Fact]
    public async Task ConcurrentCategoryWrites_ShouldAllPersist_WhenUsingIsolatedDbContexts()
    {
        const int concurrency = 10;

        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            await using var db = CreateDb();
            var repo = new CategoryRepository(db);
            var category = TestDataFactory.CreateCategory($"Concurrent Category {i}");
            await repo.AddAsync(category);
        });

        await Task.WhenAll(tasks);

        // Verify all categories were persisted
        await using var readDb = CreateDb();
        var readRepo = new CategoryRepository(readDb);
        var all = await readRepo.GetAllAsync();
        all.Should().HaveCount(concurrency);
    }

    [Fact]
    public async Task ConcurrentUserWrites_ShouldAllPersist_WhenUsingIsolatedDbContexts()
    {
        const int concurrency = 5;

        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            await using var db = CreateDb();
            var repo = new UserRepository(db);
            var user = TestDataFactory.CreateUser(
                email: $"parallel{i}@example.com",
                externalAuthId: $"auth_parallel_{i}");
            await repo.AddAsync(user);
        });

        await Task.WhenAll(tasks);

        await using var readDb = CreateDb();
        var readRepo = new UserRepository(readDb);
        var criteria = new Application.Dtos.User.UserQueryCriteria(
            SearchTerm: "parallel",
            SortField: Application.Dtos.User.UserSortField.CreatedAt,
            SortDirection: Application.Dtos.SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100,
            IsDeletedFilter: false);

        var (items, total) = await readRepo.GetPagedAsync(criteria);
        total.Should().Be(concurrency);
        items.Should().HaveCount(concurrency);
    }
}
