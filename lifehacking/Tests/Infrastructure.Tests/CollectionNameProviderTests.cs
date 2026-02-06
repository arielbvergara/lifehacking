using FluentAssertions;
using Infrastructure.Data.Firestore;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Unit tests for collection name provider implementations.
/// Validates Requirements 1.1, 1.2, 1.5 from the firestore-test-infrastructure-improvements spec.
/// </summary>
public sealed class CollectionNameProviderTests
{
    [Fact]
    public void TestCollectionNameProvider_ShouldGenerateUniqueSuffix_WhenCreated()
    {
        // Arrange & Act
        var provider1 = new TestCollectionNameProvider();
        var provider2 = new TestCollectionNameProvider();

        var name1 = provider1.GetCollectionName("users");
        var name2 = provider2.GetCollectionName("users");

        // Assert
        name1.Should().NotBe(name2, "each provider instance should generate a unique suffix");
    }

    [Fact]
    public void TestCollectionNameProvider_ShouldFollowNamingPattern_WhenGettingCollectionName()
    {
        // Arrange
        var provider = new TestCollectionNameProvider();
        const string baseCollectionName = "users";

        // Act
        var collectionName = provider.GetCollectionName(baseCollectionName);

        // Assert
        collectionName.Should().StartWith($"{baseCollectionName}_", "collection name should start with base name and underscore");
        collectionName.Should().MatchRegex(@"^users_[a-f0-9]{8}$", "collection name should follow pattern {baseCollectionName}_{8-char-hex}");
    }

    [Fact]
    public void TestCollectionNameProvider_ShouldReturnConsistentName_WhenCalledMultipleTimes()
    {
        // Arrange
        var provider = new TestCollectionNameProvider();
        const string baseCollectionName = "tips";

        // Act
        var name1 = provider.GetCollectionName(baseCollectionName);
        var name2 = provider.GetCollectionName(baseCollectionName);
        var name3 = provider.GetCollectionName(baseCollectionName);

        // Assert
        name1.Should().Be(name2, "same provider instance should return consistent names");
        name2.Should().Be(name3, "same provider instance should return consistent names");
    }

    [Fact]
    public void TestCollectionNameProvider_ShouldThrowArgumentNullException_WhenBaseCollectionNameIsNull()
    {
        // Arrange
        var provider = new TestCollectionNameProvider();

        // Act
        var act = () => provider.GetCollectionName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseCollectionName");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void TestCollectionNameProvider_ShouldThrowArgumentException_WhenBaseCollectionNameIsEmptyOrWhitespace(string invalidName)
    {
        // Arrange
        var provider = new TestCollectionNameProvider();

        // Act
        var act = () => provider.GetCollectionName(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseCollectionName")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void ProductionCollectionNameProvider_ShouldReturnBaseNameUnchanged_WhenGettingCollectionName()
    {
        // Arrange
        var provider = new ProductionCollectionNameProvider();
        const string baseCollectionName = "users";

        // Act
        var collectionName = provider.GetCollectionName(baseCollectionName);

        // Assert
        collectionName.Should().Be(baseCollectionName, "production provider should return base name unchanged");
    }

    [Fact]
    public void ProductionCollectionNameProvider_ShouldReturnConsistentName_WhenCalledMultipleTimes()
    {
        // Arrange
        var provider = new ProductionCollectionNameProvider();
        const string baseCollectionName = "categories";

        // Act
        var name1 = provider.GetCollectionName(baseCollectionName);
        var name2 = provider.GetCollectionName(baseCollectionName);

        // Assert
        name1.Should().Be(baseCollectionName);
        name2.Should().Be(baseCollectionName);
        name1.Should().Be(name2);
    }

    [Fact]
    public void ProductionCollectionNameProvider_ShouldThrowArgumentNullException_WhenBaseCollectionNameIsNull()
    {
        // Arrange
        var provider = new ProductionCollectionNameProvider();

        // Act
        var act = () => provider.GetCollectionName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseCollectionName");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ProductionCollectionNameProvider_ShouldThrowArgumentException_WhenBaseCollectionNameIsEmptyOrWhitespace(string invalidName)
    {
        // Arrange
        var provider = new ProductionCollectionNameProvider();

        // Act
        var act = () => provider.GetCollectionName(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseCollectionName")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void TestCollectionNameProvider_ShouldGenerateEightCharacterSuffix_WhenCreated()
    {
        // Arrange
        var provider = new TestCollectionNameProvider();
        const string baseCollectionName = "test";

        // Act
        var collectionName = provider.GetCollectionName(baseCollectionName);

        // Assert
        var suffix = collectionName.Substring(baseCollectionName.Length + 1); // +1 for underscore
        suffix.Should().HaveLength(8, "suffix should be exactly 8 characters");
        suffix.Should().MatchRegex("^[a-f0-9]{8}$", "suffix should be 8 hexadecimal characters");
    }

    [Fact]
    public void TestCollectionNameProvider_ShouldWorkWithDifferentBaseNames_WhenGettingCollectionName()
    {
        // Arrange
        var provider = new TestCollectionNameProvider();

        // Act
        var usersCollection = provider.GetCollectionName("users");
        var tipsCollection = provider.GetCollectionName("tips");
        var categoriesCollection = provider.GetCollectionName("categories");

        // Assert
        usersCollection.Should().StartWith("users_");
        tipsCollection.Should().StartWith("tips_");
        categoriesCollection.Should().StartWith("categories_");

        // All should have the same suffix (same provider instance)
        var usersSuffix = usersCollection.Substring("users_".Length);
        var tipsSuffix = tipsCollection.Substring("tips_".Length);
        var categoriesSuffix = categoriesCollection.Substring("categories_".Length);

        usersSuffix.Should().Be(tipsSuffix);
        tipsSuffix.Should().Be(categoriesSuffix);
    }
}
