using Application.Interfaces;
using Google.Cloud.Firestore;
using Infrastructure.Configuration;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;

namespace WebAPI.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Get FirebaseDatabaseOptions from settings
        var databaseProviderOptions = configuration
            .GetSection(FirebaseDatabaseOptions.SectionName)
            .Get<FirebaseDatabaseOptions>();

        var projectId = databaseProviderOptions?.ProjectId;

        // For testing environment, use a demo project ID if not configured
        if (environment.IsEnvironment("Testing") && string.IsNullOrWhiteSpace(projectId))
        {
            projectId = "demo-lifehacking-test";
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "Firestore database provider is configured but Firebase:ProjectId is missing.");
        }

        // Create FirestoreDb instance - will automatically connect to emulator if FIRESTORE_EMULATOR_HOST is set
        services.AddSingleton(_ => FirestoreDb.Create(projectId));

        // Register collection name provider for production (returns base collection names unchanged)
        // Only register if not already registered (tests may provide their own)
        if (services.All(d => d.ServiceType != typeof(ICollectionNameProvider)))
        {
            services.AddSingleton<ICollectionNameProvider, ProductionCollectionNameProvider>();
        }

        services.AddScoped<IFirestoreUserDataStore, FirestoreUserDataStore>();
        services.AddScoped<IFirestoreTipDataStore, FirestoreTipDataStore>();
        services.AddScoped<IFirestoreCategoryDataStore, FirestoreCategoryDataStore>();
        services.AddScoped<IFirestoreFavoriteDataStore, FirestoreFavoriteDataStore>();

        // Register Firestore-based repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITipRepository, TipRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFavoritesRepository, FavoritesRepository>();

        return services;
    }
}
