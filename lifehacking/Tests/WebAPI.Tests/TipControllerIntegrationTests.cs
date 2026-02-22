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
        tipDetail.VideoUrl.Should().BeNull();
        tipDetail.VideoUrlId.Should().BeNull();
        tipDetail.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        tipDetail.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetTipById_ShouldReturnTipDetailWithVideo_WhenTipHasVideoUrl()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create test category
        var category = Category.Create("Video Category");
        await categoryRepository.AddAsync(category);

        // Create test tip with YouTube URL
        var tip = CreateTestTipWithVideo("Video Tip", "Watch this video", category.Id, "https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _client.GetAsync($"/api/Tip/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tipDetail = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipDetail.Should().NotBeNull();
        tipDetail!.Id.Should().Be(tip.Id.Value);
        tipDetail.Title.Should().Be("Video Tip");
        tipDetail.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        tipDetail.VideoUrlId.Should().Be("dQw4w9WgXcQ");
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

    private static Tip CreateTestTipWithVideo(
        string title,
        string description,
        CategoryId categoryId,
        string videoUrl,
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
        var videoUrlObject = VideoUrl.Create(videoUrl);

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags, videoUrlObject);
    }

    private static Tip CreateTestTipWithImage(
        string title,
        string description,
        CategoryId categoryId,
        ImageMetadata image,
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

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags, image: image);
    }

    [Fact]
    public async Task SearchTips_ShouldReturnTipsWithAndWithoutImages_WhenMixedTipsExist()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create test category
        var category = Category.Create("Mixed Tips Category");
        await categoryRepository.AddAsync(category);

        // Create tip with image
        var imageWithMetadata = ImageMetadata.Create(
            imageUrl: "https://cdn.example.com/tips/test-image-1.jpg",
            imageStoragePath: "tips/2024/12/test-image-1.jpg",
            originalFileName: "test-image-1.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 1024 * 500, // 500KB
            uploadedAt: DateTime.UtcNow);

        var tipWithImage = CreateTestTipWithImage(
            "Tip With Image",
            "This tip has an associated image for visual guidance",
            category.Id,
            imageWithMetadata,
            new[] { "visual", "test" });

        await tipRepository.AddAsync(tipWithImage);

        // Create tip without image
        var tipWithoutImage = CreateTestTip(
            "Tip Without Image",
            "This tip does not have an associated image",
            category.Id,
            new[] { "text-only", "test" });

        await tipRepository.AddAsync(tipWithoutImage);

        // Create another tip with image
        var anotherImageWithMetadata = ImageMetadata.Create(
            imageUrl: "https://cdn.example.com/tips/test-image-2.png",
            imageStoragePath: "tips/2024/12/test-image-2.png",
            originalFileName: "test-image-2.png",
            contentType: "image/png",
            fileSizeBytes: 1024 * 300, // 300KB
            uploadedAt: DateTime.UtcNow);

        var anotherTipWithImage = CreateTestTipWithImage(
            "Another Tip With Image",
            "This is another tip with an image attachment",
            category.Id,
            anotherImageWithMetadata,
            new[] { "visual", "guide" });

        await tipRepository.AddAsync(anotherTipWithImage);

        // Act - Call GET /api/Tip endpoint
        var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/api/Tip?pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the GET /api/Tip endpoint should return 200 OK");

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Items.Should().HaveCountGreaterThanOrEqualTo(3,
            "the response should include at least the 3 tips we created");

        // Verify tip with image has populated Image field
        var tipWithImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Tip With Image");
        tipWithImageResponse.Should().NotBeNull("tip with image should be in the response");
        tipWithImageResponse!.Image.Should().NotBeNull("tip with image should have populated Image field");
        tipWithImageResponse.Image!.ImageUrl.Should().Be("https://cdn.example.com/tips/test-image-1.jpg");
        tipWithImageResponse.Image.ImageStoragePath.Should().Be("tips/2024/12/test-image-1.jpg");
        tipWithImageResponse.Image.OriginalFileName.Should().Be("test-image-1.jpg");
        tipWithImageResponse.Image.ContentType.Should().Be("image/jpeg");
        tipWithImageResponse.Image.FileSizeBytes.Should().Be(1024 * 500);

        // Verify another tip with image has populated Image field
        var anotherTipWithImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Another Tip With Image");
        anotherTipWithImageResponse.Should().NotBeNull("another tip with image should be in the response");
        anotherTipWithImageResponse!.Image.Should().NotBeNull("another tip with image should have populated Image field");
        anotherTipWithImageResponse.Image!.ImageUrl.Should().Be("https://cdn.example.com/tips/test-image-2.png");
        anotherTipWithImageResponse.Image.ContentType.Should().Be("image/png");
        anotherTipWithImageResponse.Image.FileSizeBytes.Should().Be(1024 * 300);

        // Verify tip without image has null Image field
        var tipWithoutImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Tip Without Image");
        tipWithoutImageResponse.Should().NotBeNull("tip without image should be in the response");
        tipWithoutImageResponse!.Image.Should().BeNull("tip without image should have null Image field");
    }
}
