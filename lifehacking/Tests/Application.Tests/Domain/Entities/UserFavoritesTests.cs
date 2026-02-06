using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.Entities;

public class UserFavoritesTests
{
    [Fact]
    public void Create_ShouldSetPropertiesCorrectly_WhenValidInputsProvided()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var favorite = UserFavorites.Create(userId, tipId);

        // Assert
        var afterCreate = DateTime.UtcNow;
        favorite.UserId.Should().Be(userId);
        favorite.TipId.Should().Be(tipId);
        favorite.AddedAt.Should().BeOnOrAfter(beforeCreate);
        favorite.AddedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Create_ShouldSetAddedAtToUtcNow_WhenCalled()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var favorite = UserFavorites.Create(userId, tipId);

        // Assert
        favorite.AddedAt.Kind.Should().Be(DateTimeKind.Utc);
        favorite.AddedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FromPersistence_ShouldRehydrateEntityCorrectly_WhenValidDataProvided()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var addedAt = DateTime.UtcNow.AddDays(-5);

        // Act
        var favorite = UserFavorites.FromPersistence(userId, tipId, addedAt);

        // Assert
        favorite.UserId.Should().Be(userId);
        favorite.TipId.Should().Be(tipId);
        favorite.AddedAt.Should().Be(addedAt);
    }

    [Fact]
    public void GetCompositeKey_ShouldReturnCorrectFormat_WhenCalled()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var favorite = UserFavorites.Create(userId, tipId);
        var expectedKey = $"{userId.Value}_{tipId.Value}";

        // Act
        var compositeKey = favorite.GetCompositeKey();

        // Assert
        compositeKey.Should().Be(expectedKey);
        compositeKey.Should().Contain("_");
        compositeKey.Should().StartWith(userId.Value.ToString());
        compositeKey.Should().EndWith(tipId.Value.ToString());
    }

    [Fact]
    public void UserFavorites_ShouldBeImmutable_WhenCreated()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var favorite = UserFavorites.Create(userId, tipId);

        // Act & Assert
        var userIdProperty = typeof(UserFavorites).GetProperty(nameof(UserFavorites.UserId));
        var tipIdProperty = typeof(UserFavorites).GetProperty(nameof(UserFavorites.TipId));
        var addedAtProperty = typeof(UserFavorites).GetProperty(nameof(UserFavorites.AddedAt));

        userIdProperty.Should().NotBeNull();
        userIdProperty!.CanWrite.Should().BeFalse("UserId should be read-only");

        tipIdProperty.Should().NotBeNull();
        tipIdProperty!.CanWrite.Should().BeFalse("TipId should be read-only");

        addedAtProperty.Should().NotBeNull();
        addedAtProperty!.CanWrite.Should().BeFalse("AddedAt should be read-only");
    }

    [Fact]
    public void Create_ShouldCreateDifferentInstances_WhenCalledMultipleTimes()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        // Act
        var favorite1 = UserFavorites.Create(userId, tipId);
        var favorite2 = UserFavorites.Create(userId, tipId);

        // Assert
        favorite1.Should().NotBeSameAs(favorite2);
        favorite1.UserId.Should().Be(favorite2.UserId);
        favorite1.TipId.Should().Be(favorite2.TipId);
    }

    [Fact]
    public void FromPersistence_ShouldPreserveExactTimestamp_WhenRehydrating()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var specificTimestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var favorite = UserFavorites.FromPersistence(userId, tipId, specificTimestamp);

        // Assert
        favorite.AddedAt.Should().Be(specificTimestamp);
        favorite.AddedAt.Year.Should().Be(2024);
        favorite.AddedAt.Month.Should().Be(1);
        favorite.AddedAt.Day.Should().Be(15);
        favorite.AddedAt.Hour.Should().Be(10);
        favorite.AddedAt.Minute.Should().Be(30);
        favorite.AddedAt.Second.Should().Be(45);
    }
}
