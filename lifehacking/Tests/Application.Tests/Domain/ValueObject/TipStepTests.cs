using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class TipStepTests
{
    [Fact]
    public void Create_ShouldCreateTipStepWithExpectedValues_WhenValidParametersProvided()
    {
        // Arrange
        var stepNumber = 1;
        var description = "Boil water in a large pot.";

        // Act
        var tipStep = TipStep.Create(stepNumber, description);

        // Assert
        tipStep.Should().NotBeNull();
        tipStep.StepNumber.Should().Be(stepNumber);
        tipStep.Description.Should().Be(description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_ShouldThrowArgumentException_WhenStepNumberLessThanOne(int invalidStepNumber)
    {
        // Arrange
        var description = "Boil water in a large pot.";

        // Act
        var act = () => TipStep.Create(invalidStepNumber, description);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Step number must be at least 1*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowArgumentException_WhenDescriptionNullOrWhitespace(string? invalidDescription)
    {
        // Arrange
        var stepNumber = 1;

        // Act
        var act = () => TipStep.Create(stepNumber, invalidDescription!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Step description cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenDescriptionTooShort()
    {
        // Arrange
        var stepNumber = 1;
        var shortDescription = "Too short";

        // Act
        var act = () => TipStep.Create(stepNumber, shortDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Step description must be at least 10 characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenDescriptionTooLong()
    {
        // Arrange
        var stepNumber = 1;
        var longDescription = new string('a', 501);

        // Act
        var act = () => TipStep.Create(stepNumber, longDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Step description cannot exceed 500 characters*");
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        // Arrange
        var stepNumber = 1;
        var descriptionWithWhitespace = "  Boil water in a large pot.  ";

        // Act
        var tipStep = TipStep.Create(stepNumber, descriptionWithWhitespace);

        // Assert
        tipStep.Description.Should().Be("Boil water in a large pot.");
    }

    [Fact]
    public void TipStep_ShouldHaveValueEquality()
    {
        // Arrange
        var stepNumber = 1;
        var description = "Boil water in a large pot.";
        var tipStep1 = TipStep.Create(stepNumber, description);
        var tipStep2 = TipStep.Create(stepNumber, description);

        // Act & Assert
        tipStep1.Should().Be(tipStep2);
        (tipStep1 == tipStep2).Should().BeTrue();
    }
}
