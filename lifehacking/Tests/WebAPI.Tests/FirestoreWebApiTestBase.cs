using Application.Interfaces;
using Infrastructure.Data.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Base class for WebAPI tests that need to interact with repositories.
/// Provides access to Firestore-based repositories through the test application factory.
/// </summary>
public abstract class FirestoreWebApiTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected ICollectionNameProvider CollectionNameProvider { get; }

    protected FirestoreWebApiTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        CollectionNameProvider = factory.CollectionNameProvider;

        // Set emulator environment variable for tests
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8080");
    }

    protected IUserRepository GetUserRepository()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    protected ITipRepository GetTipRepository()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITipRepository>();
    }

    protected ICategoryRepository GetCategoryRepository()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
    }
}
