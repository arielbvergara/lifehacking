using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class TipDescriptionTests
{
    [Fact]
    public void Create_ShouldCreateTipDescriptionWithExpectedValue_WhenValidDescriptionProvided()
    {
        // Arrange
        var description = "This is a detailed description of how to cook pasta properly.";

        // Act
        var tipDescription = TipDescription.Create(description);

        // Assert
        tipDescription.Should().NotBeNull();
        tipDescription.Value.Should().Be(description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNullOrWhitespaceProvided(string? invalidDescription)
    {
        // Act
        var act = () => TipDescription.Create(invalidDescription!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip description cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenDescriptionTooShort()
    {
        // Arrange
        var shortDescription = "Too short";

        // Act
        var act = () => TipDescription.Create(shortDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip description must be at least 10 characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenDescriptionTooLong()
    {
        // Arrange
        var longDescription = new string('a', 2001);

        // Act
        var act = () => TipDescription.Create(longDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip description cannot exceed 2000 characters*");
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        // Arrange
        var descriptionWithWhitespace = "  This is a detailed description.  ";

        // Act
        var tipDescription = TipDescription.Create(descriptionWithWhitespace);

        // Assert
        tipDescription.Value.Should().Be("This is a detailed description.");
    }

    [Fact]
    public void TipDescription_ShouldHaveValueEquality()
    {
        // Arrange
        var description = "This is a detailed description.";
        var tipDescription1 = TipDescription.Create(description);
        var tipDescription2 = TipDescription.Create(description);

        // Act & Assert
        tipDescription1.Should().Be(tipDescription2);
        (tipDescription1 == tipDescription2).Should().BeTrue();
    }
}
