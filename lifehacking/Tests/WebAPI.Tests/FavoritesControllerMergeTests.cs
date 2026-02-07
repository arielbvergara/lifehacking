using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Favorite;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

[Trait("Category", "Integration")]
public sealed class FavoritesControllerMergeTests : FirestoreWebApiTestBase
{
    public FavoritesControllerMergeTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task MergeFavorites_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = Factory.CreateClient();
        // Do not add X-Test-Only-ExternalId header to simulate missing authentication
        var requestDto = new MergeFavoritesRequestDto(new List<Guid> { Guid.NewGuid() });

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MergeFavorites_ShouldReturn400_WhenRequestBodyIsNull()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);

        // Act
        var response = await client.PostAsync("/api/me/favorites/merge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MergeFavorites_ShouldReturnSuccessWithZeroCounts_WhenEmptyListProvided()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var requestDto = new MergeFavoritesRequestDto(new List<Guid>());

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(0);
        result.Added.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeFavorites_ShouldAddAllTips_WhenAllTipsAreValidAndNew()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var tips = new List<Tip>();
        for (int i = 0; i < 5; i++)
        {
            var tip = TestDataFactory.CreateTip(category.Id, title: $"Test Tip {i}");
            await tipRepository.AddAsync(tip);
            tips.Add(tip);
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var tipIds = tips.Select(t => t.Id.Value).ToList();
        var requestDto = new MergeFavoritesRequestDto(tipIds);

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(5);
        result.Added.Should().Be(5);
        result.Skipped.Should().Be(0);
        result.Failed.Should().BeEmpty();

        // Verify favorites were actually added
        var favoritesRepository = GetFavoritesRepository();
        foreach (var tipId in tipIds)
        {
            var exists = await favoritesRepository.ExistsAsync(user.Id, TipId.Create(tipId));
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task MergeFavorites_ShouldSkipAllTips_WhenAllTipsAlreadyFavorited()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var tips = new List<Tip>();
        for (int i = 0; i < 3; i++)
        {
            var tip = TestDataFactory.CreateTip(category.Id, title: $"Test Tip {i}");
            await tipRepository.AddAsync(tip);
            tips.Add(tip);
        }

        // Add favorites first
        var favoritesRepository = GetFavoritesRepository();
        foreach (var tip in tips)
        {
            var favorite = UserFavorites.Create(user.Id, tip.Id);
            await favoritesRepository.AddAsync(favorite);
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var tipIds = tips.Select(t => t.Id.Value).ToList();
        var requestDto = new MergeFavoritesRequestDto(tipIds);

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(3);
        result.Added.Should().Be(0);
        result.Skipped.Should().Be(3);
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeFavorites_ShouldHandleMixOfNewAndExisting_WhenPartialOverlap()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var allTips = new List<Tip>();
        for (int i = 0; i < 5; i++)
        {
            var tip = TestDataFactory.CreateTip(category.Id, title: $"Test Tip {i}");
            await tipRepository.AddAsync(tip);
            allTips.Add(tip);
        }

        var existingTips = allTips.Take(2).ToList();

        // Add some favorites first
        var favoritesRepository = GetFavoritesRepository();
        foreach (var tip in existingTips)
        {
            var favorite = UserFavorites.Create(user.Id, tip.Id);
            await favoritesRepository.AddAsync(favorite);
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var tipIds = allTips.Select(t => t.Id.Value).ToList();
        var requestDto = new MergeFavoritesRequestDto(tipIds);

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(5);
        result.Added.Should().Be(3);
        result.Skipped.Should().Be(2);
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeFavorites_ShouldReportAllAsFailed_WhenAllTipsInvalid()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var invalidTipIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var requestDto = new MergeFavoritesRequestDto(invalidTipIds);

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(3);
        result.Added.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Failed.Should().HaveCount(3);
        result.Failed.Should().OnlyContain(f => f.ErrorMessage == "Tip not found");
    }

    [Fact]
    public async Task MergeFavorites_ShouldHandleMixOfValidAndInvalid_WhenPartialSuccess()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var validTips = new List<Tip>();
        for (int i = 0; i < 2; i++)
        {
            var tip = TestDataFactory.CreateTip(category.Id, title: $"Test Tip {i}");
            await tipRepository.AddAsync(tip);
            validTips.Add(tip);
        }

        var invalidTipId = Guid.NewGuid();
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var tipIds = validTips.Select(t => t.Id.Value).Append(invalidTipId).ToList();
        var requestDto = new MergeFavoritesRequestDto(tipIds);

        // Act
        var response = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result.Should().NotBeNull();
        result!.TotalReceived.Should().Be(3);
        result.Added.Should().Be(2);
        result.Skipped.Should().Be(0);
        result.Failed.Should().HaveCount(1);
        result.Failed.First().TipId.Should().Be(invalidTipId);
        result.Failed.First().ErrorMessage.Should().Be("Tip not found");
    }

    [Fact]
    public async Task MergeFavorites_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tipRepository = GetTipRepository();
        var tips = new List<Tip>();
        for (int i = 0; i < 3; i++)
        {
            var tip = TestDataFactory.CreateTip(category.Id, title: $"Test Tip {i}");
            await tipRepository.AddAsync(tip);
            tips.Add(tip);
        }

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        var tipIds = tips.Select(t => t.Id.Value).ToList();
        var requestDto = new MergeFavoritesRequestDto(tipIds);

        // Act - Call merge twice
        var response1 = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);
        var response2 = await client.PostAsJsonAsync("/api/me/favorites/merge", requestDto);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await response1.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result1!.Added.Should().Be(3);
        result1.Skipped.Should().Be(0);

        var result2 = await response2.Content.ReadFromJsonAsync<MergeFavoritesResponse>();
        result2!.Added.Should().Be(0);
        result2.Skipped.Should().Be(3);

        // Verify only 3 favorites exist (not 6)
        var favoritesRepository = GetFavoritesRepository();
        var favoritesCount = 0;
        foreach (var tipId in tipIds)
        {
            var exists = await favoritesRepository.ExistsAsync(user.Id, TipId.Create(tipId));
            if (exists) favoritesCount++;
        }
        favoritesCount.Should().Be(3);
    }

    [Fact]
    public async Task MergeFavorites_ShouldHandleInvalidGuidFormat_WhenMalformedGuidProvided()
    {
        // Arrange
        var userRepository = GetUserRepository();
        var user = TestDataFactory.CreateUser();
        await userRepository.AddAsync(user);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", user.ExternalAuthId.Value);
        
        // Create JSON with invalid GUID manually
        var json = """{"tipIds":["not-a-guid"]}""";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/me/favorites/merge", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private IFavoritesRepository GetFavoritesRepository()
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<Application.Interfaces.IFavoritesRepository>();
    }
}
