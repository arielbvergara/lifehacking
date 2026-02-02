using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class CategoryIdTests
{
    [Fact]
    public void Create_ShouldCreateCategoryIdWithExpectedValue_WhenValidGuidProvided()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var categoryId = CategoryId.Create(guid);

        // Assert
        categoryId.Should().NotBeNull();
        categoryId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenEmptyGuidProvided()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var act = () => CategoryId.Create(emptyGuid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category ID cannot be empty*");
    }

    [Fact]
    public void NewId_ShouldGenerateNonEmptyGuid()
    {
        // Act
        var categoryId = CategoryId.NewId();

        // Assert
        categoryId.Should().NotBeNull();
        categoryId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CategoryId_ShouldHaveValueEquality()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var categoryId1 = CategoryId.Create(guid);
        var categoryId2 = CategoryId.Create(guid);

        // Act & Assert
        categoryId1.Should().Be(categoryId2);
        (categoryId1 == categoryId2).Should().BeTrue();
    }
}
