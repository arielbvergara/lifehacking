using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObject;

public class TipIdTests
{
    [Fact]
    public void Create_ShouldCreateTipIdWithExpectedValue_WhenValidGuidProvided()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var tipId = TipId.Create(guid);

        // Assert
        tipId.Should().NotBeNull();
        tipId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenEmptyGuidProvided()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var act = () => TipId.Create(emptyGuid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip ID cannot be empty*");
    }

    [Fact]
    public void NewId_ShouldGenerateNonEmptyGuid()
    {
        // Act
        var tipId = TipId.NewId();

        // Assert
        tipId.Should().NotBeNull();
        tipId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TipId_ShouldHaveValueEquality()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var tipId1 = TipId.Create(guid);
        var tipId2 = TipId.Create(guid);

        // Act & Assert
        tipId1.Should().Be(tipId2);
        (tipId1 == tipId2).Should().BeTrue();
    }
}
