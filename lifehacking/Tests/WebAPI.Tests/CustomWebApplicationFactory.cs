using System.Threading.RateLimiting;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using WebAPI.Authentication;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// TEST-ONLY WebApplicationFactory used by WebAPI.Tests to host the API in-memory.
/// It overrides authentication with TestAuthHandler, replaces the database with a
/// real PostgreSQL Testcontainer, and disables rate limiting for fast tests.
/// DO NOT use this in production code paths.
/// </summary>
/// <remarks>
/// For tests that need to verify rate limiting behavior, use <see cref="RateLimitingWebApplicationFactory"/> instead.
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("lifehacking_webapi_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    /// <summary>
    /// Controls whether rate limiting should be disabled for tests.
    /// Override this in derived classes to preserve rate limiting behavior.
    /// </summary>
    protected virtual bool DisableRateLimiting => true;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Create the schema so WebAPI integration tests have a live database.
        // Program.cs skips Database.Migrate() in the "Testing" environment,
        // so we bootstrap it here directly.
        var options = new DbContextOptionsBuilder<LifehackingDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new LifehackingDbContext(options);
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ClientApp:Origin"] = "http://localhost",
                ["ConnectionStrings:DbContext"] = _postgres.GetConnectionString(),
            };
            configurationBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            // Replace DbContext registration with the Testcontainer connection string
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LifehackingDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            var dbDescriptor2 = services.SingleOrDefault(
                d => d.ServiceType == typeof(LifehackingDbContext));
            if (dbDescriptor2 is not null)
                services.Remove(dbDescriptor2);

            services.AddDbContext<LifehackingDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));

            // Override authentication with a lightweight test scheme.
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            // Replace the real Firebase admin client with a test double that does not
            // require Google Application Default Credentials.
            var firebaseDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFirebaseAdminClient));
            if (firebaseDescriptor is not null)
                services.Remove(firebaseDescriptor);

            var idpDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IIdentityProviderService));
            if (idpDescriptor is not null)
                services.Remove(idpDescriptor);

            services.AddSingleton<IFirebaseAdminClient, TestFirebaseAdminClient>();
            services.AddSingleton<IIdentityProviderService>(sp => sp.GetRequiredService<IFirebaseAdminClient>());

            // Replace the logging security event notifier with a test double.
            var securityNotifierDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISecurityEventNotifier));
            if (securityNotifierDescriptor is not null)
                services.Remove(securityNotifierDescriptor);

            services.AddSingleton<ISecurityEventNotifier, TestSecurityEventNotifier>();

            // Replace the real image storage service with a test double.
            var imageStorageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IImageStorageService));
            if (imageStorageDescriptor is not null)
                services.Remove(imageStorageDescriptor);

            services.AddSingleton<IImageStorageService, TestImageStorageService>();

            if (DisableRateLimiting)
            {
                var rateLimiterDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.Contains("RateLimiterOptions"));

                if (rateLimiterDescriptor != null)
                    services.Remove(rateLimiterDescriptor);

                services.Configure<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>(options =>
                {
                    options.AddPolicy("fixed", _ =>
                        RateLimitPartition.GetNoLimiter<string>("test"));

                    options.AddPolicy("strict", _ =>
                        RateLimitPartition.GetNoLimiter<string>("test"));
                });
            }
        });
    }

    private sealed class TestFirebaseAdminClient : IFirebaseAdminClient
    {
        public Task<string> EnsureAdminUserAsync(
            string email,
            string password,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult($"test-{normalizedEmail}");
        }

        public Task DeleteUserAsync(string externalAuthId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestImageStorageService : IImageStorageService
    {
        public Task<ImageStorageResult> UploadAsync(
            Stream fileStream,
            string originalFileName,
            string contentType,
            string pathPrefix = "categories",
            CancellationToken cancellationToken = default)
        {
            var sanitizedFileName = originalFileName.Replace(" ", "_").Replace("..", "");
            var storagePath = $"{pathPrefix}/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{Guid.NewGuid()}{Path.GetExtension(sanitizedFileName)}";
            var cdnUrl = $"https://test-cdn.example.com/{storagePath}";

            return Task.FromResult(new ImageStorageResult(
                StoragePath: storagePath,
                PublicUrl: cdnUrl));
        }
    }
}

public sealed class TestSecurityEventNotifier : ISecurityEventNotifier
{
    public sealed record SecurityEventRecord(
        string EventName,
        string? SubjectId,
        string Outcome,
        string? CorrelationId,
        IReadOnlyDictionary<string, string?>? Properties);

    private readonly List<SecurityEventRecord> _events = new();

    public IReadOnlyList<SecurityEventRecord> Events => _events;

    public void ClearEvents() => _events.Clear();

    public Task NotifyAsync(
        string eventName,
        string? subjectId,
        string outcome,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        _events.Add(new SecurityEventRecord(eventName, subjectId, outcome, correlationId, properties));
        return Task.CompletedTask;
    }
}
