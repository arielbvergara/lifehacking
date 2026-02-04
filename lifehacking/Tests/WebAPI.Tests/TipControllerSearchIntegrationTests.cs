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

public class TipControllerSearchIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SearchTips_ShouldReturnEmptyResults_WhenNoMatchingTipsExist()
    {
        // Act - Search for something that definitely doesn't exist
        var response = await _client.GetAsync("/api/Tip?q=nonexistentuniquetestterm12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().BeEmpty();
        searchResults.Metadata.TotalItems.Should().Be(0);
        searchResults.Metadata.PageNumber.Should().Be(1);
        searchResults.Metadata.PageSize.Should().Be(20);
        searchResults.Metadata.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task SearchTips_ShouldReturnTips_WhenNoFiltersApplied()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueCategoryName = $"SearchTest-{Guid.NewGuid():N}";
        var category = Category.Create(uniqueCategoryName);
        await categoryRepository.AddAsync(category);

        // Create unique test tips
        var tip1 = CreateTestTip($"SearchTip1-{Guid.NewGuid():N}", "This is the first search tip", category.Id, new[] { "search1", "common" });
        var tip2 = CreateTestTip($"SearchTip2-{Guid.NewGuid():N}", "This is the second search tip", category.Id, new[] { "search2", "common" });
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);

        // Act - Search for tips with the unique category
        var response = await _client.GetAsync($"/api/Tip?categoryId={category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(2);
        searchResults.Metadata.TotalItems.Should().Be(2);
        searchResults.Metadata.PageNumber.Should().Be(1);
        searchResults.Metadata.PageSize.Should().Be(20);
        searchResults.Metadata.TotalPages.Should().Be(1);

        // Verify tips are sorted by CreatedAt descending (default)
        searchResults.Items[0].Title.Should().StartWith("SearchTip2-"); // Created later
        searchResults.Items[1].Title.Should().StartWith("SearchTip1-");
    }

    [Fact]
    public async Task SearchTips_ShouldFilterBySearchTerm_WhenSearchTermProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueCategoryName = $"SearchTermTest-{Guid.NewGuid():N}";
        var category = Category.Create(uniqueCategoryName);
        await categoryRepository.AddAsync(category);

        // Create unique test tips with specific search terms
        var uniqueSearchTerm = $"uniquecookingterm{Guid.NewGuid():N}";
        var tip1 = CreateTestTip($"Tips for {uniqueSearchTerm}", "Learn how to cook pasta", category.Id);
        var tip2 = CreateTestTip("Cleaning Guide", "How to clean your house", category.Id);
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);

        // Act
        var response = await _client.GetAsync($"/api/Tip?q={uniqueSearchTerm}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items[0].Title.Should().Contain(uniqueSearchTerm);
        searchResults.Metadata.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task SearchTips_ShouldFilterByCategory_WhenCategoryIdProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test categories
        var uniqueId = Guid.NewGuid().ToString("N");
        var category1 = Category.Create($"CategoryFilter1-{uniqueId}");
        var category2 = Category.Create($"CategoryFilter2-{uniqueId}");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        // Create test tips
        var tip1 = CreateTestTip($"Tip in Category 1-{uniqueId}", "Description 1", category1.Id);
        var tip2 = CreateTestTip($"Tip in Category 2-{uniqueId}", "Description 2", category2.Id);
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);

        // Act
        var response = await _client.GetAsync($"/api/Tip?categoryId={category1.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items[0].Title.Should().Contain($"Category 1-{uniqueId}");
        searchResults.Items[0].CategoryName.Should().Be($"CategoryFilter1-{uniqueId}");
        searchResults.Metadata.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task SearchTips_ShouldFilterByTags_WhenTagsProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueId = Guid.NewGuid().ToString("N");
        var category = Category.Create($"TagTest-{uniqueId}");
        await categoryRepository.AddAsync(category);

        // Create unique tag names
        var uniqueTag1 = $"uniquetag1{uniqueId}";
        var uniqueTag2 = $"uniquetag2{uniqueId}";
        var uniqueTag3 = $"uniquetag3{uniqueId}";

        // Create test tips with different tags
        var tip1 = CreateTestTip($"Tip 1-{uniqueId}", "Description 1", category.Id, new[] { uniqueTag1, "beginner" });
        var tip2 = CreateTestTip($"Tip 2-{uniqueId}", "Description 2", category.Id, new[] { uniqueTag2, "advanced" });
        var tip3 = CreateTestTip($"Tip 3-{uniqueId}", "Description 3", category.Id, new[] { uniqueTag1, "advanced" });
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);
        await tipRepository.AddAsync(tip3);

        // Act - Filter by unique tag
        var response = await _client.GetAsync($"/api/Tip?tags={uniqueTag1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(2);
        searchResults.Items.Should().OnlyContain(t => t.Tags.Contains(uniqueTag1));
        searchResults.Metadata.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task SearchTips_ShouldFilterByMultipleTags_WhenMultipleTagsProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueId = Guid.NewGuid().ToString("N");
        var category = Category.Create($"MultiTagTest-{uniqueId}");
        await categoryRepository.AddAsync(category);

        // Create unique tag names
        var uniqueTag1 = $"multitag1{uniqueId}";
        var uniqueTag2 = $"multitag2{uniqueId}";
        var uniqueTag3 = $"multitag3{uniqueId}";

        // Create test tips with different tag combinations
        var tip1 = CreateTestTip($"Tip 1-{uniqueId}", "Description 1", category.Id, new[] { uniqueTag1, "beginner" });
        var tip2 = CreateTestTip($"Tip 2-{uniqueId}", "Description 2", category.Id, new[] { uniqueTag2, "advanced" });
        var tip3 = CreateTestTip($"Tip 3-{uniqueId}", "Description 3", category.Id, new[] { uniqueTag1, uniqueTag3 });
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);
        await tipRepository.AddAsync(tip3);

        // Act - Filter by multiple unique tags
        var response = await _client.GetAsync($"/api/Tip?tags={uniqueTag1}&tags={uniqueTag3}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items[0].Title.Should().Contain($"Tip 3-{uniqueId}");
        searchResults.Items[0].Tags.Should().Contain(uniqueTag1);
        searchResults.Items[0].Tags.Should().Contain(uniqueTag3);
        searchResults.Metadata.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task SearchTips_ShouldSortByTitle_WhenOrderByTitleSpecified()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueId = Guid.NewGuid().ToString("N");
        var category = Category.Create($"SortTest-{uniqueId}");
        await categoryRepository.AddAsync(category);

        // Create test tips with different titles (using unique prefix to avoid conflicts)
        var tip1 = CreateTestTip($"ZZZ-{uniqueId}", "Description Z", category.Id);
        var tip2 = CreateTestTip($"AAA-{uniqueId}", "Description A", category.Id);
        var tip3 = CreateTestTip($"BBB-{uniqueId}", "Description B", category.Id);
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);
        await tipRepository.AddAsync(tip3);

        // Act - Sort by title ascending and filter by category to get only our test tips
        var response = await _client.GetAsync($"/api/Tip?categoryId={category.Id.Value}&orderBy=Title&sortDirection=Ascending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(3);
        searchResults.Items[0].Title.Should().StartWith($"AAA-{uniqueId}");
        searchResults.Items[1].Title.Should().StartWith($"BBB-{uniqueId}");
        searchResults.Items[2].Title.Should().StartWith($"ZZZ-{uniqueId}");
    }

    [Fact]
    public async Task SearchTips_ShouldHandlePagination_WhenPageParametersProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueId = Guid.NewGuid().ToString("N");
        var category = Category.Create($"PaginationTest-{uniqueId}");
        await categoryRepository.AddAsync(category);

        // Create exactly 25 test tips
        for (int i = 1; i <= 25; i++)
        {
            var tip = CreateTestTip($"PaginationTip-{uniqueId}-{i:D2}", $"Description {i}", category.Id);
            await tipRepository.AddAsync(tip);
        }

        // Act - Get second page with 10 items per page, filtered by our unique category
        var response = await _client.GetAsync($"/api/Tip?categoryId={category.Id.Value}&pageNumber=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(10);
        searchResults.Metadata.TotalItems.Should().Be(25);
        searchResults.Metadata.PageNumber.Should().Be(2);
        searchResults.Metadata.PageSize.Should().Be(10);
        searchResults.Metadata.TotalPages.Should().Be(3); // Ceiling(25/10) = 3
    }

    [Fact]
    public async Task SearchTips_ShouldReturnBadRequest_WhenPageNumberIsInvalid()
    {
        // Act
        var response = await _client.GetAsync("/api/Tip?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Page number must be greater than or equal to 1");
    }

    [Fact]
    public async Task SearchTips_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Act - Test page size too large
        var response1 = await _client.GetAsync("/api/Tip?pageSize=101");
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content1 = await response1.Content.ReadAsStringAsync();
        content1.Should().Contain("Page size must be between 1 and 100");

        // Act - Test page size too small
        var response2 = await _client.GetAsync("/api/Tip?pageSize=0");
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Contain("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task SearchTips_ShouldBePubliclyAccessible_WhenNoAuthenticationProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test category
        var uniqueId = Guid.NewGuid().ToString("N");
        var category = Category.Create($"PublicTest-{uniqueId}");
        await categoryRepository.AddAsync(category);

        // Create unique test tip
        var tip = CreateTestTip($"PublicSearchTip-{uniqueId}", "This should be searchable without authentication", category.Id);
        await tipRepository.AddAsync(tip);

        // Create a new client without any authentication headers
        var publicClient = factory.CreateClient();

        // Act - Filter by our unique category
        var response = await publicClient.GetAsync($"/api/Tip?categoryId={category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items[0].Title.Should().StartWith($"PublicSearchTip-{uniqueId}");
    }

    [Fact]
    public async Task SearchTips_ShouldCombineFilters_WhenMultipleFiltersProvided()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var tipRepository = scope.ServiceProvider.GetRequiredService<ITipRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        // Create unique test categories
        var uniqueId = Guid.NewGuid().ToString("N");
        var category1 = Category.Create($"CombinedTest1-{uniqueId}");
        var category2 = Category.Create($"CombinedTest2-{uniqueId}");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        // Create unique search term and tag
        var uniqueSearchTerm = $"uniquepasta{uniqueId}";
        var uniqueTag = $"uniquebeginner{uniqueId}";

        // Create test tips
        var tip1 = CreateTestTip($"{uniqueSearchTerm} Cooking Guide", "How to cook pasta perfectly", category1.Id, new[] { uniqueTag, "italian" });
        var tip2 = CreateTestTip($"Advanced {uniqueSearchTerm} Techniques", "Professional pasta cooking", category1.Id, new[] { "advanced", "italian" });
        var tip3 = CreateTestTip("Kitchen Cleaning", "How to clean your kitchen", category2.Id, new[] { uniqueTag, "cleaning" });
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);
        await tipRepository.AddAsync(tip3);

        // Act - Combine search term, category, and tag filters
        var response = await _client.GetAsync($"/api/Tip?q={uniqueSearchTerm}&categoryId={category1.Id.Value}&tags={uniqueTag}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await response.Content.ReadFromJsonAsync<PagedTipsResponse>();
        searchResults.Should().NotBeNull();
        searchResults!.Items.Should().HaveCount(1);
        searchResults.Items[0].Title.Should().Contain($"{uniqueSearchTerm} Cooking Guide");
        searchResults.Metadata.TotalItems.Should().Be(1);
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
}