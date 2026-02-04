using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data.InMemory;
using Infrastructure.Repositories.InMemory;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public sealed class CategoryRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ICategoryRepository _repository;

    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new InMemoryCategoryRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCategory_WhenValidCategoryProvided()
    {
        // Arrange
        var category = Category.Create("Test Category");

        // Act
        var result = await _repository.AddAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);

        var persistedCategory = await _repository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = CategoryId.NewId();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var category = Category.Create("Test Category");
        await _repository.AddAsync(category);

        // Act
        var result = await _repository.GetByIdAsync(category.Id);

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

        await _repository.AddAsync(category1);
        await _repository.AddAsync(category2);
        await _repository.AddAsync(category3);

        // Act
        var result = await _repository.GetAllAsync();

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
        var result = await _repository.GetAllAsync();

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

        await _repository.AddAsync(category1);
        await _repository.AddAsync(category2);
        await _repository.AddAsync(category3);

        // Act
        var result = await _repository.GetAllAsync();

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
        await _repository.AddAsync(category);

        // Act
        var result = await _repository.GetByNameAsync("Unique Category");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Category");
        result.Id.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNull_WhenCategoryWithNameDoesNotExist()
    {
        // Act
        var result = await _repository.GetByNameAsync("Non-existent Category");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_ShouldHandleWhitespace_WhenSearchingByName()
    {
        // Arrange
        var category = Category.Create("Test Category");
        await _repository.AddAsync(category);

        // Act
        var result = await _repository.GetByNameAsync("  Test Category  ");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Category");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory_WhenValidChangesProvided()
    {
        // Arrange
        var category = Category.Create("Original Name");
        await _repository.AddAsync(category);

        category.UpdateName("Updated Name");

        // Act
        await _repository.UpdateAsync(category);

        // Assert
        var updatedCategory = await _repository.GetByIdAsync(category.Id);
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Updated Name");
        updatedCategory.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory_WhenCategoryExists()
    {
        // Arrange
        var category = Category.Create("Category to Delete");
        await _repository.AddAsync(category);

        // Act
        await _repository.DeleteAsync(category.Id);

        // Assert
        var deletedCategory = await _repository.GetByIdAsync(category.Id);
        deletedCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = CategoryId.NewId();

        // Act & Assert
        var act = async () => await _repository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveCreatedAt_WhenCategoryAdded()
    {
        // Arrange
        var category = Category.Create("Time Test Category");
        var beforeAdd = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await _repository.AddAsync(category);
        var afterAdd = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var persistedCategory = await _repository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.CreatedAt.Should().BeAfter(beforeAdd);
        persistedCategory.CreatedAt.Should().BeBefore(afterAdd);
        persistedCategory.UpdatedAt.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
