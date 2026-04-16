using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Base class for WebAPI tests that need to interact with repositories via the test application factory.
/// </summary>
public abstract class FirestoreWebApiTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected FirestoreWebApiTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
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
