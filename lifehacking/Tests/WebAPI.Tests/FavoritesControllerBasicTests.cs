using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Favorite;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Basic integration tests for FavoritesController to verify setup is correct.
/// </summary>
public sealed class FavoritesControllerBasicTests : FirestoreWebApiTestBase
{
    public FavoritesControllerBasicTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyFavorites_ShouldReturn200WithEmptyList_WhenUserHasNoFavorites()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);

        // Act
        var response = await client.GetAsync("/api/me/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedFavoritesResponse>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Favorites.Should().BeEmpty();
        pagedResponse.Metadata.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task GetMyFavorites_ShouldReturn200WithFavorites_WhenUserHasFavorites()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(category.Id, title: "Test Tip");
        await tipRepository.AddAsync(tip);

        var favoritesRepository = GetFavoritesRepository();
        var favorite = UserFavorites.Create(user.Id, tip.Id);
        await favoritesRepository.AddAsync(favorite);

        // Verify the favorite was actually stored
        var storedFavorite = await favoritesRepository.GetByUserAndTipAsync(user.Id, tip.Id);
        storedFavorite.Should().NotBeNull("favorite should be stored in the repository");

        // Small delay to ensure Firestore emulator has persisted the data
        await Task.Delay(100);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);

        // Act
        var response = await client.GetAsync("/api/me/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedFavoritesResponse>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Favorites.Should().HaveCount(1);
        pagedResponse.Favorites[0].TipId.Should().Be(tip.Id.Value);
        pagedResponse.Favorites[0].TipDetails.Title.Should().Be("Test Tip");
        pagedResponse.Metadata.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetMyFavorites_ShouldReturn401Unauthorized_WhenExternalAuthIdIsMissing()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Do not add X-Test-Only-ExternalId header to simulate missing authentication

        // Act
        var response = await client.GetAsync("/api/me/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyFavorites_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Use an external auth ID that doesn't correspond to any user in the system
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "non-existent-user-id");

        // Act
        var response = await client.GetAsync("/api/me/favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // POST endpoint edge case tests

    [Fact]
    public async Task AddFavorite_ShouldReturn404NotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);

        // Use a tip ID that doesn't exist in the system
        var nonExistentTipId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/me/favorites/{nonExistentTipId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "adding a non-existent tip should return 404 Not Found");
    }

    [Fact]
    public async Task AddFavorite_ShouldReturn401Unauthorized_WhenExternalAuthIdIsMissing()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Do not add X-Test-Only-ExternalId header to simulate missing authentication

        var tipId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/me/favorites/{tipId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "request without authentication should return 401 Unauthorized");
    }

    [Fact]
    public async Task AddFavorite_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Use an external auth ID that doesn't correspond to any user in the system
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "non-existent-user-id");

        var tipId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/me/favorites/{tipId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "request with non-existent user should return 404 Not Found");
    }

    // DELETE endpoint edge case tests

    [Fact]
    public async Task RemoveFavorite_ShouldReturn404NotFound_WhenFavoriteDoesNotExist()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);

        // Use a tip ID that is not in the user's favorites
        var nonExistentTipId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/me/favorites/{nonExistentTipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "removing a non-existent favorite should return 404 Not Found");
    }

    [Fact]
    public async Task RemoveFavorite_ShouldReturn401Unauthorized_WhenExternalAuthIdIsMissing()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Do not add X-Test-Only-ExternalId header to simulate missing authentication

        var tipId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/me/favorites/{tipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "request without authentication should return 401 Unauthorized");
    }

    [Fact]
    public async Task RemoveFavorite_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Use an external auth ID that doesn't correspond to any user in the system
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "non-existent-user-id");

        var tipId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/me/favorites/{tipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "request with non-existent user should return 404 Not Found");
    }

    private IFavoritesRepository GetFavoritesRepository()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IFavoritesRepository>();
    }
}
