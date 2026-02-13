using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.Tests;

public class CategoryControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetCategories_ShouldReturn200_WhenCategoriesExist()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var category1 = Category.Create("Technology");
        var category2 = Category.Create("Health");
        var category3 = Category.Create("Finance");

        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);
        await categoryRepository.AddAsync(category3);

        // Act
        var response = await _client.GetAsync("/api/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryList = await response.Content.ReadFromJsonAsync<CategoryListResponse>();
        categoryList.Should().NotBeNull();
        categoryList!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        categoryList.Items.Should().Contain(c => c.Name == "Technology");
        categoryList.Items.Should().Contain(c => c.Name == "Health");
        categoryList.Items.Should().Contain(c => c.Name == "Finance");
    }

    [Fact]
    public async Task GetCategories_ShouldReturn200WithEmptyArray_WhenNoCategoriesExist()
    {
        // Arrange
        // Use a fresh factory instance to ensure no categories exist
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Clean up any existing categories
        var existingCategories = await categoryRepository.GetAllAsync();
        foreach (var category in existingCategories)
        {
            await categoryRepository.DeleteAsync(category.Id);
        }

        // Act
        var response = await _client.GetAsync("/api/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryList = await response.Content.ReadFromJsonAsync<CategoryListResponse>();
        categoryList.Should().NotBeNull();
        categoryList!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_ShouldAllowAnonymousAccess_WhenNoAuthProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var category = Category.Create("Public Category");
        await categoryRepository.AddAsync(category);

        // Create a new client without any authentication headers
        var publicClient = factory.CreateClient();

        // Act
        var response = await publicClient.GetAsync("/api/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryList = await response.Content.ReadFromJsonAsync<CategoryListResponse>();
        categoryList.Should().NotBeNull();
        categoryList!.Items.Should().Contain(c => c.Name == "Public Category");
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn200_WhenCategoryExists()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();

        var category = Category.Create("Test Category");
        await categoryRepository.AddAsync(category);

        var tip1 = CreateTestTip("Tip 1", "Description 1", category.Id);
        var tip2 = CreateTestTip("Tip 2", "Description 2", category.Id);

        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);

        // Act
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedTips = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedTips.Should().NotBeNull();
        pagedTips!.Items.Should().HaveCount(2);
        pagedTips.Items.Should().Contain(t => t.Title == "Tip 1");
        pagedTips.Items.Should().Contain(t => t.Title == "Tip 2");
        pagedTips.Metadata.TotalItems.Should().Be(2);
        pagedTips.Metadata.PageNumber.Should().Be(1);
        pagedTips.Metadata.PageSize.Should().Be(10);
        pagedTips.Metadata.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn200WithEmptyArray_WhenCategoryHasNoTips()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var emptyCategory = Category.Create("Empty Category");
        await categoryRepository.AddAsync(emptyCategory);

        // Act
        var response = await _client.GetAsync($"/api/Category/{emptyCategory.Id.Value}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedTips = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedTips.Should().NotBeNull();
        pagedTips!.Items.Should().BeEmpty();
        pagedTips.Metadata.TotalItems.Should().Be(0);
        pagedTips.Metadata.PageNumber.Should().Be(1);
        pagedTips.Metadata.PageSize.Should().Be(10);
        pagedTips.Metadata.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentCategoryId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/Category/{nonExistentCategoryId}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Category with ID '{nonExistentCategoryId}' not found");
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn404_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var category = Category.Create("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        // Act
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn400_WhenCategoryIdIsInvalid()
    {
        // Arrange
        const string invalidCategoryId = "not-a-guid";

        // Act
        var response = await _client.GetAsync($"/api/Category/{invalidCategoryId}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid category ID format");
        content.Should().Contain(invalidCategoryId);
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn400_WhenPageNumberIsInvalid()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var category = Category.Create("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Page number must be greater than or equal to 1");
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturn400_WhenPageSizeIsInvalid()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var category = Category.Create("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips?pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturnPaginatedResults_WhenValidParametersProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();

        var category = Category.Create("Paginated Category");
        await categoryRepository.AddAsync(category);

        // Create 5 tips
        for (int i = 1; i <= 5; i++)
        {
            var tip = CreateTestTip($"Tip {i}", $"Description {i}", category.Id);
            await tipRepository.AddAsync(tip);
        }

        // Act - Request page 2 with page size 2
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips?pageNumber=2&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedTips = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedTips.Should().NotBeNull();
        pagedTips!.Items.Should().HaveCount(2);
        pagedTips.Metadata.TotalItems.Should().Be(5);
        pagedTips.Metadata.PageNumber.Should().Be(2);
        pagedTips.Metadata.PageSize.Should().Be(2);
        pagedTips.Metadata.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldReturnSortedResults_WhenSortParametersProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();

        var category = Category.Create("Sorted Category");
        await categoryRepository.AddAsync(category);

        var tip1 = CreateTestTip("Zebra Tip", "Description Z", category.Id);
        var tip2 = CreateTestTip("Alpha Tip", "Description A", category.Id);
        var tip3 = CreateTestTip("Beta Tip", "Description B", category.Id);

        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);
        await tipRepository.AddAsync(tip3);

        // Act - Sort by title ascending
        var response = await _client.GetAsync($"/api/Category/{category.Id.Value}/tips?orderBy=Title&sortDirection=Ascending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedTips = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedTips.Should().NotBeNull();
        pagedTips!.Items.Should().HaveCount(3);
        pagedTips.Items[0].Title.Should().Be("Alpha Tip");
        pagedTips.Items[1].Title.Should().Be("Beta Tip");
        pagedTips.Items[2].Title.Should().Be("Zebra Tip");
    }

    [Fact]
    public async Task GetTipsByCategory_ShouldAllowAnonymousAccess_WhenNoAuthProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();

        var category = Category.Create("Public Category");
        await categoryRepository.AddAsync(category);

        var tip = CreateTestTip("Public Tip", "Public Description", category.Id);
        await tipRepository.AddAsync(tip);

        // Create a new client without any authentication headers
        var publicClient = factory.CreateClient();

        // Act
        var response = await publicClient.GetAsync($"/api/Category/{category.Id.Value}/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedTips = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedTips.Should().NotBeNull();
        pagedTips!.Items.Should().HaveCount(1);
        pagedTips.Items[0].Title.Should().Be("Public Tip");
    }
    [Fact]
    public async Task GetTipsByCategory_ShouldReturnTipsWithAndWithoutImages_WhenMixedTipsExist()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();

        // Create test category
        var category = Category.Create("Mixed Tips Category");
        await categoryRepository.AddAsync(category);

        // Create tip with image
        var imageWithMetadata = TipImage.Create(
            imageUrl: "https://cdn.example.com/tips/category-test-image-1.jpg",
            imageStoragePath: "tips/2024/12/category-test-image-1.jpg",
            originalFileName: "category-test-image-1.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 1024 * 600, // 600KB
            uploadedAt: DateTime.UtcNow);

        var tipWithImage = CreateTestTipWithImage(
            "Category Tip With Image",
            "This tip has an associated image for visual guidance",
            category.Id,
            imageWithMetadata,
            new[] { "visual", "category-test" });

        await tipRepository.AddAsync(tipWithImage);

        // Create tip without image
        var tipWithoutImage = CreateTestTip(
            "Category Tip Without Image",
            "This tip does not have an associated image",
            category.Id,
            new[] { "text-only", "category-test" });

        await tipRepository.AddAsync(tipWithoutImage);

        // Create another tip with image
        var anotherImageWithMetadata = TipImage.Create(
            imageUrl: "https://cdn.example.com/tips/category-test-image-2.png",
            imageStoragePath: "tips/2024/12/category-test-image-2.png",
            originalFileName: "category-test-image-2.png",
            contentType: "image/png",
            fileSizeBytes: 1024 * 400, // 400KB
            uploadedAt: DateTime.UtcNow);

        var anotherTipWithImage = CreateTestTipWithImage(
            "Another Category Tip With Image",
            "This is another tip with an image attachment",
            category.Id,
            anotherImageWithMetadata,
            new[] { "visual", "guide" });

        await tipRepository.AddAsync(anotherTipWithImage);

        // Act - Call GET /api/Category/{id}/tips endpoint
        var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync($"/api/Category/{category.Id.Value}/tips?pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the GET /api/Category/{id}/tips endpoint should return 200 OK");

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Items.Should().HaveCount(3,
            "the response should include exactly the 3 tips we created for this category");

        // Verify tip with image has populated Image field
        var tipWithImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Category Tip With Image");
        tipWithImageResponse.Should().NotBeNull("tip with image should be in the response");
        tipWithImageResponse!.Image.Should().NotBeNull("tip with image should have populated Image field");
        tipWithImageResponse.Image!.ImageUrl.Should().Be("https://cdn.example.com/tips/category-test-image-1.jpg");
        tipWithImageResponse.Image.ImageStoragePath.Should().Be("tips/2024/12/category-test-image-1.jpg");
        tipWithImageResponse.Image.OriginalFileName.Should().Be("category-test-image-1.jpg");
        tipWithImageResponse.Image.ContentType.Should().Be("image/jpeg");
        tipWithImageResponse.Image.FileSizeBytes.Should().Be(1024 * 600);

        // Verify another tip with image has populated Image field
        var anotherTipWithImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Another Category Tip With Image");
        anotherTipWithImageResponse.Should().NotBeNull("another tip with image should be in the response");
        anotherTipWithImageResponse!.Image.Should().NotBeNull("another tip with image should have populated Image field");
        anotherTipWithImageResponse.Image!.ImageUrl.Should().Be("https://cdn.example.com/tips/category-test-image-2.png");
        anotherTipWithImageResponse.Image.ContentType.Should().Be("image/png");
        anotherTipWithImageResponse.Image.FileSizeBytes.Should().Be(1024 * 400);

        // Verify tip without image has null Image field
        var tipWithoutImageResponse = pagedResponse.Items.FirstOrDefault(t => t.Title == "Category Tip Without Image");
        tipWithoutImageResponse.Should().NotBeNull("tip without image should be in the response");
        tipWithoutImageResponse!.Image.Should().BeNull("tip without image should have null Image field");
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
    private static Tip CreateTestTipWithImage(
        string title,
        string description,
        CategoryId categoryId,
        TipImage image,
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

}
