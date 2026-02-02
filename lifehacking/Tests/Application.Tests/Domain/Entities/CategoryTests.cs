using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.Entities;

public class CategoryTests
{
    [Fact]
    public void Create_ShouldCreateCategoryWithExpectedValues_WhenValidNameProvided()
    {
        // Arrange
        var name = "Cooking";
        var before = DateTime.UtcNow;

        // Act
        var category = Category.Create(name);
        var after = DateTime.UtcNow;

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be(name);

        category.Id.Should().NotBe(null);
        category.Id.Value.Should().NotBe(Guid.Empty);

        category.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        category.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNameNullOrWhitespace(string? invalidName)
    {
        // Act
        var act = () => Category.Create(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenNameTooShort()
    {
        // Arrange
        var shortName = "A";

        // Act
        var act = () => Category.Create(shortName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name must be at least 2 characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenNameTooLong()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => Category.Create(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name cannot exceed 100 characters*");
    }

    [Fact]
    public void UpdateName_ShouldUpdateNameAndSetUpdatedAt_WhenValidNameProvided()
    {
        // Arrange
        var category = Category.Create("Cooking");
        var originalCreatedAt = category.CreatedAt;
        var newName = "Baking";
        var before = DateTime.UtcNow;

        // Act
        category.UpdateName(newName);
        var after = DateTime.UtcNow;

        // Assert
        category.Name.Should().Be(newName);
        category.CreatedAt.Should().Be(originalCreatedAt);
        category.UpdatedAt.Should().NotBeNull();
        category.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_ShouldThrowArgumentException_WhenInvalidNameProvided(string? invalidName)
    {
        // Arrange
        var category = Category.Create("Cooking");

        // Act
        var act = () => category.UpdateName(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
    }

    [Fact]
    public void FromPersistence_ShouldRehydrateCategoryCorrectly()
    {
        // Arrange
        var id = CategoryId.NewId();
        var name = "Cooking";
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var category = Category.FromPersistence(id, name, createdAt, updatedAt);

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().Be(id);
        category.Name.Should().Be(name);
        category.CreatedAt.Should().Be(createdAt);
        category.UpdatedAt.Should().Be(updatedAt);
    }
}
