using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class TagTests
{
    [Fact]
    public void Create_ShouldCreateTagWithExpectedValue_WhenValidTagProvided()
    {
        // Arrange
        var tag = "cooking";

        // Act
        var tagValue = Tag.Create(tag);

        // Assert
        tagValue.Should().NotBeNull();
        tagValue.Value.Should().Be(tag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNullOrWhitespaceProvided(string? invalidTag)
    {
        // Act
        var act = () => Tag.Create(invalidTag!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tag cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTagEmptyAfterTrim()
    {
        // Arrange
        var emptyAfterTrim = "   ";

        // Act
        var act = () => Tag.Create(emptyAfterTrim);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tag cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTagTooLong()
    {
        // Arrange
        var longTag = new string('a', 51);

        // Act
        var act = () => Tag.Create(longTag);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tag cannot exceed 50 characters*");
    }

    [Fact]
    public void Create_ShouldTrimTag()
    {
        // Arrange
        var tagWithWhitespace = "  cooking  ";

        // Act
        var tag = Tag.Create(tagWithWhitespace);

        // Assert
        tag.Value.Should().Be("cooking");
    }

    [Fact]
    public void Tag_ShouldHaveValueEquality()
    {
        // Arrange
        var tagValue = "cooking";
        var tag1 = Tag.Create(tagValue);
        var tag2 = Tag.Create(tagValue);

        // Act & Assert
        tag1.Should().Be(tag2);
        (tag1 == tag2).Should().BeTrue();
    }
}
