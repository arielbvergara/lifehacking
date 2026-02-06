using Application.Dtos;
using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class TipRepositoryTests : FirestoreTestBase
{
    private readonly Category _testCategory;

    public TipRepositoryTests()
    {
        // Clean up any existing test data before each test
        CleanupTestDataAsync().Wait();

        // Create a test category first
        _testCategory = Category.Create("Test Category");
        // Add the category to the repository so it exists for tip creation
        CategoryRepository.AddAsync(_testCategory).Wait();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTip_WhenValidTipProvided()
    {
        // Arrange
        var tip = CreateTestTip();

        // Act
        var result = await TipRepository.AddAsync(tip);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tip.Id);

        var persistedTip = await TipRepository.GetByIdAsync(tip.Id);
        persistedTip.Should().NotBeNull();
        persistedTip!.Title.Value.Should().Be(tip.Title.Value);
        persistedTip.Description.Value.Should().Be(tip.Description.Value);
        persistedTip.Steps.Should().HaveCount(tip.Steps.Count);
        persistedTip.Tags.Should().HaveCount(tip.Tags.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTipDoesNotExist()
    {
        // Arrange
        var nonExistentId = TipId.NewId();

        // Act
        var result = await TipRepository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTip_WhenTipExists()
    {
        // Arrange
        var tip = CreateTestTip();
        await TipRepository.AddAsync(tip);

        // Act
        var result = await TipRepository.GetByIdAsync(tip.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tip.Id);
        result.Title.Value.Should().Be(tip.Title.Value);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredTips_WhenSearchTermProvided()
    {
        // Arrange
        var tip1 = CreateTestTip("Cooking Tips", "How to cook pasta");
        var tip2 = CreateTestTip("Cleaning Guide", "How to cook and clean kitchen");
        var tip3 = CreateTestTip("Gardening", "How to plant flowers");

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        var criteria = new TipQueryCriteria(
            SearchTerm: "cook",
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().Contain(t => t.Title.Value == "Cooking Tips");
        items.Should().Contain(t => t.Title.Value == "Cleaning Guide");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredTips_WhenCategoryFilterProvided()
    {
        // Arrange
        var anotherCategory = Category.Create("Another Category");
        await CategoryRepository.AddAsync(anotherCategory);

        var tip1 = CreateTestTip("Tip 1", "Description 1", _testCategory.Id);
        var tip2 = CreateTestTip("Tip 2", "Description 2", anotherCategory.Id);
        var tip3 = CreateTestTip("Tip 3", "Description 3", _testCategory.Id);

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: _testCategory.Id.Value,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(t => t.CategoryId.Should().Be(_testCategory.Id));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredTips_WhenTagsFilterProvided()
    {
        // Arrange
        var tip1 = CreateTestTip("Tip 1", "Description 1", tags: new[] { "cooking", "easy" });
        var tip2 = CreateTestTip("Tip 2", "Description 2", tags: new[] { "cleaning", "quick" });
        var tip3 = CreateTestTip("Tip 3", "Description 3", tags: new[] { "cooking", "advanced" });

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: new[] { "cooking" },
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(t => t.Tags.Should().Contain(tag => tag.Value == "cooking"));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnPagedResults_WhenPaginationProvided()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            var tip = CreateTestTip($"Tip {i}", $"Description {i}");
            await TipRepository.AddAsync(tip);
        }

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.Title,
            SortDirection: SortDirection.Ascending,
            PageNumber: 2,
            PageSize: 2);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnTipsInCategory_WhenCategoryExists()
    {
        // Arrange
        var anotherCategory = Category.Create("Another Category");
        await CategoryRepository.AddAsync(anotherCategory);

        var tip1 = CreateTestTip("Tip 1", "Description 1", _testCategory.Id);
        var tip2 = CreateTestTip("Tip 2", "Description 2", anotherCategory.Id);
        var tip3 = CreateTestTip("Tip 3", "Description 3", _testCategory.Id);

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        // Act
        var result = await TipRepository.GetByCategoryAsync(_testCategory.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.CategoryId.Should().Be(_testCategory.Id));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenCategoryHasNoTips()
    {
        // Arrange
        var emptyCategory = Category.Create("Empty Category");
        await CategoryRepository.AddAsync(emptyCategory);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: emptyCategory.Id.Value,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldExcludeDeletedTips_WhenFilteringByCategory()
    {
        // Arrange
        var tip1 = CreateTestTip("Active Tip", "Description 1", _testCategory.Id);
        var tip2 = CreateTestTip("Deleted Tip", "Description 2", _testCategory.Id);
        var tip3 = CreateTestTip("Another Active Tip", "Description 3", _testCategory.Id);

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        // Soft delete tip2
        await TipRepository.DeleteAsync(tip2.Id);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: _testCategory.Id.Value,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(t => t.Id == tip2.Id);
    }

    [Fact]
    public async Task SearchAsync_ShouldApplyPaginationCorrectly_WhenFilteringByCategory()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            var tip = CreateTestTip($"Category Tip {i}", $"Description {i}", _testCategory.Id);
            await TipRepository.AddAsync(tip);
        }

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: _testCategory.Id.Value,
            Tags: null,
            SortField: TipSortField.Title,
            SortDirection: SortDirection.Ascending,
            PageNumber: 2,
            PageSize: 2);

        // Act
        var (items, totalCount) = await TipRepository.SearchAsync(criteria);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(t => t.CategoryId.Should().Be(_testCategory.Id));
    }

    [Fact]
    public async Task SearchAsync_ShouldApplySortingCorrectly_WhenFilteringByCategory()
    {
        // Arrange
        var tip1 = CreateTestTip("Zebra Tip", "Description 1", _testCategory.Id);
        var tip2 = CreateTestTip("Alpha Tip", "Description 2", _testCategory.Id);
        var tip3 = CreateTestTip("Beta Tip", "Description 3", _testCategory.Id);

        await TipRepository.AddAsync(tip1);
        await TipRepository.AddAsync(tip2);
        await TipRepository.AddAsync(tip3);

        var criteriaAsc = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: _testCategory.Id.Value,
            Tags: null,
            SortField: TipSortField.Title,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 10);

        // Act
        var (itemsAsc, _) = await TipRepository.SearchAsync(criteriaAsc);

        // Assert
        itemsAsc.Should().HaveCount(3);
        itemsAsc.First().Title.Value.Should().Be("Alpha Tip");
        itemsAsc.Last().Title.Value.Should().Be("Zebra Tip");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTip_WhenValidChangesProvided()
    {
        // Arrange
        var tip = CreateTestTip();
        await TipRepository.AddAsync(tip);

        var newTitle = TipTitle.Create("Updated Title");
        tip.UpdateTitle(newTitle);

        // Act
        await TipRepository.UpdateAsync(tip);

        // Assert
        var updatedTip = await TipRepository.GetByIdAsync(tip.Id);
        updatedTip.Should().NotBeNull();
        updatedTip!.Title.Value.Should().Be("Updated Title");
        updatedTip.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTip_WhenTipExists()
    {
        // Arrange
        var tip = CreateTestTip();
        await TipRepository.AddAsync(tip);

        // Act
        await TipRepository.DeleteAsync(tip.Id);

        // Assert
        var deletedTip = await TipRepository.GetByIdAsync(tip.Id);
        deletedTip.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenTipDoesNotExist()
    {
        // Arrange
        var nonExistentId = TipId.NewId();

        // Act & Assert
        var act = async () => await TipRepository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    private Tip CreateTestTip(
        string title = "Test Tip",
        string description = "This is a test tip description for testing purposes.",
        CategoryId? categoryId = null,
        string[]? tags = null)
    {
        var tipTitle = TipTitle.Create(title);
        var tipDescription = TipDescription.Create(description);
        var steps = new[]
        {
            TipStep.Create(1, "First step of the tip"),
            TipStep.Create(2, "Second step of the tip")
        };
        var tipCategoryId = categoryId ?? _testCategory.Id;
        var tipTags = tags?.Select(Tag.Create).ToArray() ?? new[] { Tag.Create("test") };

        return Tip.Create(tipTitle, tipDescription, steps, tipCategoryId, tipTags);
    }
}
