using Application.Dtos.Category;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Dtos;

public class CategoryExtensionsTests
{
    [Fact]
    public void ToCategoryResponse_ShouldMapCorrectly_WhenValidCategoryProvided()
    {
        // Arrange
        var category = Category.Create("Cooking");

        // Act
        var result = category.ToCategoryResponse();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id.Value);
        result.Name.Should().Be(category.Name);
        result.CreatedAt.Should().Be(category.CreatedAt);
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ToCategoryResponse_ShouldIncludeUpdatedAt_WhenCategoryUpdated()
    {
        // Arrange
        var category = Category.Create("Cooking");
        category.UpdateName("Baking");

        // Act
        var result = category.ToCategoryResponse();

        // Assert
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().Be(category.UpdatedAt);
    }

    [Fact]
    public void ToCategoryResponse_ShouldThrowArgumentNullException_WhenCategoryNull()
    {
        // Arrange
        Category? nullCategory = null;

        // Act
        var act = () => nullCategory!.ToCategoryResponse();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
