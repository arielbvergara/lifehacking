using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Base class for repository integration tests using a real PostgreSQL container.
/// Each test class gets its own schema-isolated DbContext; tables are truncated between tests.
/// </summary>
[Collection("PostgresCollection")]
public abstract class PostgresTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    protected LifehackingDbContext Db { get; private set; } = null!;
    protected UserRepository UserRepository { get; private set; } = null!;
    protected TipRepository TipRepository { get; private set; } = null!;
    protected CategoryRepository CategoryRepository { get; private set; } = null!;
    protected FavoritesRepository FavoritesRepository { get; private set; } = null!;

    protected PostgresTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<LifehackingDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        Db = new LifehackingDbContext(options);
        await Db.Database.EnsureCreatedAsync();
        await TruncateTablesAsync();

        UserRepository = new UserRepository(Db);
        TipRepository = new TipRepository(Db);
        CategoryRepository = new CategoryRepository(Db);
        FavoritesRepository = new FavoritesRepository(Db);
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
    }

    private async Task TruncateTablesAsync()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            TRUNCATE TABLE user_favorites, tips, categories, users RESTART IDENTITY CASCADE;
            """;
        await cmd.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Shared Postgres container fixture — started once per test collection.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("lifehacking_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("PostgresCollection")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }
