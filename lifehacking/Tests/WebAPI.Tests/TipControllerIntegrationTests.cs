using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

public class TipControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetTipById_ShouldReturnTipDetail_WhenTipExists()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create test category
        var category = Category.Create("Test Category");
        await categoryRepository.AddAsync(category);

        // Create test tip
        var tip = CreateTestTip("Integration Test Tip", "This is a test tip for integration testing", category.Id);
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _client.GetAsync($"/api/Tip/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tipDetail = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipDetail.Should().NotBeNull();
        tipDetail!.Id.Should().Be(tip.Id.Value);
        tipDetail.Title.Should().Be("Integration Test Tip");
        tipDetail.Description.Should().Be("This is a test tip for integration testing");
        tipDetail.CategoryId.Should().Be(category.Id.Value);
        tipDetail.CategoryName.Should().Be("Test Category");
        tipDetail.Steps.Should().HaveCount(2);
        tipDetail.Steps[0].StepNumber.Should().Be(1);
        tipDetail.Steps[0].Description.Should().Be("This is the first step of the tip with enough characters");
        tipDetail.Steps[1].StepNumber.Should().Be(2);
        tipDetail.Steps[1].Description.Should().Be("This is the second step of the tip with enough characters");
        tipDetail.Tags.Should().HaveCount(1);
        tipDetail.Tags.Should().Contain("test");
        tipDetail.YouTubeUrl.Should().BeNull();
        tipDetail.YouTubeVideoId.Should().BeNull();
        tipDetail.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        tipDetail.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetTipById_ShouldReturnTipDetailWithYouTube_WhenTipHasYouTubeUrl()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create test category
        var category = Category.Create("Video Category");
        await categoryRepository.AddAsync(category);

        // Create test tip with YouTube URL
        var tip = CreateTestTipWithYouTube("Video Tip", "Watch this video", category.Id, "https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _client.GetAsync($"/api/Tip/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tipDetail = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipDetail.Should().NotBeNull();
        tipDetail!.Id.Should().Be(tip.Id.Value);
        tipDetail.Title.Should().Be("Video Tip");
        tipDetail.YouTubeUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        tipDetail.YouTubeVideoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public async Task GetTipById_ShouldReturnNotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var nonExistentTipId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/Tip/{nonExistentTipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Tip with ID '{nonExistentTipId}' was not found");
    }

    [Fact]
    public async Task GetTipById_ShouldReturnBadRequest_WhenTipIdIsInvalid()
    {
        // Arrange
        const string invalidTipId = "not-a-guid";

        // Act
        var response = await _client.GetAsync($"/api/Tip/{invalidTipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid tip ID format");
        content.Should().Contain("not-a-guid");
    }

    [Fact]
    public async Task GetTipById_ShouldBePubliclyAccessible_WhenNoAuthenticationProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create test category
        var category = Category.Create("Public Category");
        await categoryRepository.AddAsync(category);

        // Create test tip
        var tip = CreateTestTip("Public Tip", "This tip should be accessible without authentication", category.Id);
        await tipRepository.AddAsync(tip);

        // Create a new client without any authentication headers
        var publicClient = factory.CreateClient();

        // Act
        var response = await publicClient.GetAsync($"/api/Tip/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tipDetail = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipDetail.Should().NotBeNull();
        tipDetail!.Id.Should().Be(tip.Id.Value);
        tipDetail.Title.Should().Be("Public Tip");
    }

    private static Tip CreateTestTip(
        string title,
        string description,
        CategoryId categoryId,
        string[]? tags = null)
    {
        var tipTitle = TipTitle.Create(title);
        var tipDescription = TipDescription.Create(description);
        var steps = new[]
        {
            TipStep.Create(1, "This is the first step of the tip with enough characters"),
            TipStep.Create(2, "This is the second step of the tip with enough characters")
        };
        var tipTags = tags?.Select(Tag.Create).ToArray() ?? new[] { Tag.Create("test") };

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags);
    }

    private static Tip CreateTestTipWithYouTube(
        string title,
        string description,
        CategoryId categoryId,
        string youtubeUrl,
        string[]? tags = null)
    {
        var tipTitle = TipTitle.Create(title);
        var tipDescription = TipDescription.Create(description);
        var steps = new[]
        {
            TipStep.Create(1, "Watch the video and follow along carefully"),
            TipStep.Create(2, "Follow along with the detailed instructions provided")
        };
        var tipTags = tags?.Select(Tag.Create).ToArray() ?? new[] { Tag.Create("video") };
        var videoUrl = VideoUrl.Create(youtubeUrl);

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags, videoUrl);
    }
}
