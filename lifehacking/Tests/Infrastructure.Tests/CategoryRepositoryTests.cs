using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class CategoryRepositoryTests : FirestoreTestBase
{
    public CategoryRepositoryTests()
    {
        // Clean up any existing test data before each test
        CleanupTestDataAsync().Wait();
    }
    [Fact]
    public async Task AddAsync_ShouldPersistCategory_WhenValidCategoryProvided()
    {
        // Arrange
        var category = Category.Create("Test Category");

        // Act
        var result = await CategoryRepository.AddAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);

        var persistedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = CategoryId.NewId();

        // Act
        var result = await CategoryRepository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var category = Category.Create("Test Category");
        await CategoryRepository.AddAsync(category);

        // Act
        var result = await CategoryRepository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories_WhenCategoriesExist()
    {
        // Arrange
        var category1 = Category.Create("Category A");
        var category2 = Category.Create("Category B");
        var category3 = Category.Create("Category C");

        await CategoryRepository.AddAsync(category1);
        await CategoryRepository.AddAsync(category2);
        await CategoryRepository.AddAsync(category3);

        // Act
        var result = await CategoryRepository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Name == "Category A");
        result.Should().Contain(c => c.Name == "Category B");
        result.Should().Contain(c => c.Name == "Category C");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyCollection_WhenNoCategoriesExist()
    {
        // Act
        var result = await CategoryRepository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCategoriesOrderedByName()
    {
        // Arrange
        var category1 = Category.Create("Zebra");
        var category2 = Category.Create("Alpha");
        var category3 = Category.Create("Beta");

        await CategoryRepository.AddAsync(category1);
        await CategoryRepository.AddAsync(category2);
        await CategoryRepository.AddAsync(category3);

        // Act
        var result = await CategoryRepository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        var orderedNames = result.Select(c => c.Name).ToArray();
        orderedNames.Should().BeInAscendingOrder();
        orderedNames[0].Should().Be("Alpha");
        orderedNames[1].Should().Be("Beta");
        orderedNames[2].Should().Be("Zebra");
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnCategory_WhenCategoryWithNameExists()
    {
        // Arrange
        var category = Category.Create("Unique Category");
        await CategoryRepository.AddAsync(category);

        // Act
        var result = await CategoryRepository.GetByNameAsync("Unique Category");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Category");
        result.Id.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNull_WhenCategoryWithNameDoesNotExist()
    {
        // Act
        var result = await CategoryRepository.GetByNameAsync("Non-existent Category");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_ShouldHandleWhitespace_WhenSearchingByName()
    {
        // Arrange
        var category = Category.Create("Test Category");
        await CategoryRepository.AddAsync(category);

        // Act
        var result = await CategoryRepository.GetByNameAsync("  Test Category  ");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Category");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory_WhenValidChangesProvided()
    {
        // Arrange
        var category = Category.Create("Original Name");
        await CategoryRepository.AddAsync(category);

        category.UpdateName("Updated Name");

        // Act
        await CategoryRepository.UpdateAsync(category);

        // Assert
        var updatedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Updated Name");
        updatedCategory.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory_WhenCategoryExists()
    {
        // Arrange
        var category = Category.Create("Category to Delete");
        await CategoryRepository.AddAsync(category);

        // Act
        await CategoryRepository.DeleteAsync(category.Id);

        // Assert
        var deletedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        deletedCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = CategoryId.NewId();

        // Act & Assert
        var act = async () => await CategoryRepository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveCreatedAt_WhenCategoryAdded()
    {
        // Arrange
        var category = Category.Create("Time Test Category");
        var beforeAdd = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await CategoryRepository.AddAsync(category);
        var afterAdd = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var persistedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.CreatedAt.Should().BeAfter(beforeAdd);
        persistedCategory.CreatedAt.Should().BeBefore(afterAdd);
        persistedCategory.UpdatedAt.Should().BeNull();
    }
}
