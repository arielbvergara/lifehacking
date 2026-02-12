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

    [Fact]
    public async Task AddAsync_ShouldPersistCategoryWithImage_WhenImageMetadataProvided()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image = CategoryImage.Create(
            "https://cdn.example.com/test-image.jpg",
            "categories/test-image.jpg",
            "test-image.jpg",
            "image/jpeg",
            1024000,
            uploadedAt);

        var category = Category.Create("Category with Image", image);

        // Act
        var result = await CategoryRepository.AddAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);
        result.Image.Should().NotBeNull();

        var persistedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.Name.Should().Be("Category with Image");
        persistedCategory.Image.Should().NotBeNull();
        persistedCategory.Image!.ImageUrl.Should().Be("https://cdn.example.com/test-image.jpg");
        persistedCategory.Image.ImageStoragePath.Should().Be("categories/test-image.jpg");
        persistedCategory.Image.OriginalFileName.Should().Be("test-image.jpg");
        persistedCategory.Image.ContentType.Should().Be("image/jpeg");
        persistedCategory.Image.FileSizeBytes.Should().Be(1024000);
        persistedCategory.Image.UploadedAt.Should().BeCloseTo(uploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCategoryWithoutImage_WhenNoImageProvided()
    {
        // Arrange
        var category = Category.Create("Category without Image");

        // Act
        var result = await CategoryRepository.AddAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);
        result.Image.Should().BeNull();

        var persistedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        persistedCategory.Should().NotBeNull();
        persistedCategory!.Name.Should().Be("Category without Image");
        persistedCategory.Image.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategoryWithImage_WhenCategoryHasImage()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image = CategoryImage.Create(
            "https://cdn.example.com/another-image.png",
            "categories/another-image.png",
            "another-image.png",
            "image/png",
            2048000,
            uploadedAt);

        var category = Category.Create("Category with PNG", image);
        await CategoryRepository.AddAsync(category);

        // Act
        var result = await CategoryRepository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Image.Should().NotBeNull();
        result.Image!.ImageUrl.Should().Be("https://cdn.example.com/another-image.png");
        result.Image.ContentType.Should().Be("image/png");
        result.Image.FileSizeBytes.Should().Be(2048000);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMixOfCategoriesWithAndWithoutImages()
    {
        // Arrange
        var categoryWithoutImage = Category.Create("No Image Category");

        var image = CategoryImage.Create(
            "https://cdn.example.com/image.jpg",
            "categories/image.jpg",
            "image.jpg",
            "image/jpeg",
            512000,
            DateTime.UtcNow);
        var categoryWithImage = Category.Create("Image Category", image);

        await CategoryRepository.AddAsync(categoryWithoutImage);
        await CategoryRepository.AddAsync(categoryWithImage);

        // Act
        var result = await CategoryRepository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);

        var withoutImage = result.FirstOrDefault(c => c.Name == "No Image Category");
        withoutImage.Should().NotBeNull();
        withoutImage!.Image.Should().BeNull();

        var withImage = result.FirstOrDefault(c => c.Name == "Image Category");
        withImage.Should().NotBeNull();
        withImage!.Image.Should().NotBeNull();
        withImage.Image!.ImageUrl.Should().Be("https://cdn.example.com/image.jpg");
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreserveImage_WhenCategoryUpdated()
    {
        // Arrange
        var image = CategoryImage.Create(
            "https://cdn.example.com/original.jpg",
            "categories/original.jpg",
            "original.jpg",
            "image/jpeg",
            1024000,
            DateTime.UtcNow);

        var category = Category.Create("Original Name", image);
        await CategoryRepository.AddAsync(category);

        category.UpdateName("Updated Name");

        // Act
        await CategoryRepository.UpdateAsync(category);

        // Assert
        var updatedCategory = await CategoryRepository.GetByIdAsync(category.Id);
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Updated Name");
        updatedCategory.Image.Should().NotBeNull();
        updatedCategory.Image!.ImageUrl.Should().Be("https://cdn.example.com/original.jpg");
        updatedCategory.Image.ContentType.Should().Be("image/jpeg");
    }
}
