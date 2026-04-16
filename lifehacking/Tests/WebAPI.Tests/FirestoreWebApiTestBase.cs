using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Base class for WebAPI tests that need to interact with repositories via the test application factory.
/// A single DI scope is created for the lifetime of each test instance, ensuring the
/// EF Core DbContext is not disposed prematurely between helper calls.
/// </summary>
public abstract class FirestoreWebApiTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected FirestoreWebApiTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
    }

    protected IUserRepository GetUserRepository()
        => _scope.ServiceProvider.GetRequiredService<IUserRepository>();

    protected ITipRepository GetTipRepository()
        => _scope.ServiceProvider.GetRequiredService<ITipRepository>();

    protected ICategoryRepository GetCategoryRepository()
        => _scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

    protected IFavoritesRepository GetFavoritesRepository()
        => _scope.ServiceProvider.GetRequiredService<IFavoritesRepository>();

    public void Dispose() => _scope.Dispose();
}
