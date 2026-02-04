using Application.Interfaces;
using Google.Cloud.Firestore;
using Infrastructure.Configuration;
using Infrastructure.Data.Firestore;
using Infrastructure.Data.InMemory;
using Infrastructure.Repositories;
using Infrastructure.Repositories.InMemory;

namespace WebAPI.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Always use in-memory database for the Testing environment, regardless of configuration
        var useInMemoryDb = environment.IsEnvironment("Testing");
        if (useInMemoryDb)
        {
            // In-memory EF Core database for tests and specific development scenarios.
            services.AddInMemoryDatabase();
            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<ITipRepository, InMemoryTipRepository>();
            services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();
            return services;
        }

        // Get FirebaseDatabaseOptions from settings
        var databaseProviderOptions = configuration
            .GetSection(FirebaseDatabaseOptions.SectionName)
            .Get<FirebaseDatabaseOptions>();

        var projectId = databaseProviderOptions?.ProjectId;

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "Firestore database provider is configured but Database:FirestoreProjectId is missing.");
        }

        services.AddSingleton(_ => FirestoreDb.Create(projectId));
        services.AddScoped<IFirestoreUserDataStore, FirestoreUserDataStore>();
        services.AddScoped<IFirestoreTipDataStore, FirestoreTipDataStore>();
        services.AddScoped<IFirestoreCategoryDataStore, FirestoreCategoryDataStore>();

        // Override the default EF-based repository registration when Firestore is selected
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITipRepository, TipRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}
