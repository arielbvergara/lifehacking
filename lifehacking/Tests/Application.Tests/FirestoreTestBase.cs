using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Base class for application layer integration tests backed by a real PostgreSQL container.
/// </summary>
[Collection("AppPostgresCollection")]
public abstract class FirestoreTestBase : IAsyncLifetime
{
    private readonly AppPostgresFixture _fixture;
    private LifehackingDbContext _db = null!;

    protected IUserRepository UserRepository { get; private set; } = null!;
    protected ITipRepository TipRepository { get; private set; } = null!;
    protected ICategoryRepository CategoryRepository { get; private set; } = null!;

    protected FirestoreTestBase(AppPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<LifehackingDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _db = new LifehackingDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        await TruncateTablesAsync();

        UserRepository = new UserRepository(_db);
        TipRepository = new TipRepository(_db);
        CategoryRepository = new CategoryRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
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

public sealed class AppPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("lifehacking_app_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("AppPostgresCollection")]
public sealed class AppPostgresCollection : ICollectionFixture<AppPostgresFixture> { }
