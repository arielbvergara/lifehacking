using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class TipTitleTests
{
    [Fact]
    public void Create_ShouldCreateTipTitleWithExpectedValue_WhenValidTitleProvided()
    {
        // Arrange
        var title = "How to cook pasta";

        // Act
        var tipTitle = TipTitle.Create(title);

        // Assert
        tipTitle.Should().NotBeNull();
        tipTitle.Value.Should().Be(title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenNullOrWhitespaceProvided(string? invalidTitle)
    {
        // Act
        var act = () => TipTitle.Create(invalidTitle!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip title cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTitleTooShort()
    {
        // Arrange
        var shortTitle = "Test";

        // Act
        var act = () => TipTitle.Create(shortTitle);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip title must be at least 5 characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTitleTooLong()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act
        var act = () => TipTitle.Create(longTitle);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip title cannot exceed 200 characters*");
    }

    [Fact]
    public void Create_ShouldTrimTitle()
    {
        // Arrange
        var titleWithWhitespace = "  How to cook pasta  ";

        // Act
        var tipTitle = TipTitle.Create(titleWithWhitespace);

        // Assert
        tipTitle.Value.Should().Be("How to cook pasta");
    }

    [Fact]
    public void TipTitle_ShouldHaveValueEquality()
    {
        // Arrange
        var title = "How to cook pasta";
        var tipTitle1 = TipTitle.Create(title);
        var tipTitle2 = TipTitle.Create(title);

        // Act & Assert
        tipTitle1.Should().Be(tipTitle2);
        (tipTitle1 == tipTitle2).Should().BeTrue();
    }
}
