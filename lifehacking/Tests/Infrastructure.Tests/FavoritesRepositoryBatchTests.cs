using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class FavoritesRepositoryBatchTests : FirestoreTestBase
{
    private readonly User _testUser;
    private readonly Category _testCategory;
    private readonly List<Tip> _testTips;

    public FavoritesRepositoryBatchTests()
    {
        // Clean up any existing test data before each test
        CleanupTestDataAsync().Wait();

        // Create test user
        _testUser = User.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("test-auth-id"));
        UserRepository.AddAsync(_testUser).Wait();

        // Create test category
        _testCategory = Category.Create("Test Category");
        CategoryRepository.AddAsync(_testCategory).Wait();

        // Create test tips
        _testTips = new List<Tip>();
        for (int i = 0; i < 15; i++)
        {
            var tip = CreateTestTip($"Test Tip {i}");
            TipRepository.AddAsync(tip).Wait();
            _testTips.Add(tip);
        }
    }

    [Fact]
    public async Task GetExistingFavoritesAsync_ShouldReturnEmptySet_WhenEmptyInputProvided()
    {
        // Arrange
        var emptyTipIds = Array.Empty<TipId>();

        // Act
        var result = await FavoritesRepository.GetExistingFavoritesAsync(_testUser.Id, emptyTipIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingFavoritesAsync_ShouldReturnEmptySet_WhenUserHasNoFavorites()
    {
        // Arrange
        var tipIds = _testTips.Take(5).Select(t => t.Id).ToList();

        // Act
        var result = await FavoritesRepository.GetExistingFavoritesAsync(_testUser.Id, tipIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingFavoritesAsync_ShouldReturnMatchingFavorites_WhenUserHasSomeFavorites()
    {
        // Arrange
        var favoritedTips = _testTips.Take(3).ToList();
        foreach (var tip in favoritedTips)
        {
            var favorite = UserFavorites.Create(_testUser.Id, tip.Id);
            await FavoritesRepository.AddAsync(favorite);
        }

        var queryTipIds = _testTips.Take(5).Select(t => t.Id).ToList();

        // Act
        var result = await FavoritesRepository.GetExistingFavoritesAsync(_testUser.Id, queryTipIds);

        // Assert
        result.Should().HaveCount(3);
        foreach (var favoritedTip in favoritedTips)
        {
            result.Should().Contain(favoritedTip.Id);
        }
    }

    [Fact]
    public async Task GetExistingFavoritesAsync_ShouldHandleLargeBatches_WhenMoreThan10IdsProvided()
    {
        // Arrange - Add 12 favorites to test batching (Firestore WhereIn max is 10)
        var favoritedTips = _testTips.Take(12).ToList();
        foreach (var tip in favoritedTips)
        {
            var favorite = UserFavorites.Create(_testUser.Id, tip.Id);
            await FavoritesRepository.AddAsync(favorite);
        }

        var queryTipIds = _testTips.Select(t => t.Id).ToList();

        // Act
        var result = await FavoritesRepository.GetExistingFavoritesAsync(_testUser.Id, queryTipIds);

        // Assert
        result.Should().HaveCount(12);
        foreach (var favoritedTip in favoritedTips)
        {
            result.Should().Contain(favoritedTip.Id);
        }
    }

    [Fact]
    public async Task AddBatchAsync_ShouldReturnEmptyList_WhenEmptyInputProvided()
    {
        // Arrange
        var emptyTipIds = Array.Empty<TipId>();

        // Act
        var result = await FavoritesRepository.AddBatchAsync(_testUser.Id, emptyTipIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddBatchAsync_ShouldAddAllFavorites_WhenValidTipIdsProvided()
    {
        // Arrange
        var tipIds = _testTips.Take(5).Select(t => t.Id).ToList();

        // Act
        var result = await FavoritesRepository.AddBatchAsync(_testUser.Id, tipIds);

        // Assert
        result.Should().HaveCount(5);
        foreach (var tipId in tipIds)
        {
            var exists = await FavoritesRepository.ExistsAsync(_testUser.Id, tipId);
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task AddBatchAsync_ShouldHandleLargeBatches_WhenMoreThan500IdsProvided()
    {
        // Arrange - Create 550 tips to test batch write batching (Firestore batch write max is 500)
        var largeTipList = new List<Tip>();
        for (int i = 0; i < 550; i++)
        {
            var tip = CreateTestTip($"Large Batch Tip {i}");
            await TipRepository.AddAsync(tip);
            largeTipList.Add(tip);
        }

        var tipIds = largeTipList.Select(t => t.Id).ToList();

        // Act
        var result = await FavoritesRepository.AddBatchAsync(_testUser.Id, tipIds);

        // Assert
        result.Should().HaveCount(550);
        
        // Verify a sample of favorites were actually persisted
        var sampleTipIds = tipIds.Take(10).ToList();
        foreach (var tipId in sampleTipIds)
        {
            var exists = await FavoritesRepository.ExistsAsync(_testUser.Id, tipId);
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task AddBatchAsync_ShouldCreateFavoritesWithCorrectTimestamps_WhenCalled()
    {
        // Arrange
        var tipIds = _testTips.Take(3).Select(t => t.Id).ToList();
        var beforeAdd = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await FavoritesRepository.AddBatchAsync(_testUser.Id, tipIds);
        var afterAdd = DateTime.UtcNow.AddSeconds(1);

        // Assert
        result.Should().HaveCount(3);
        foreach (var favorite in result)
        {
            favorite.AddedAt.Should().BeAfter(beforeAdd);
            favorite.AddedAt.Should().BeBefore(afterAdd);
        }
    }

    private Tip CreateTestTip(string title)
    {
        var tipTitle = TipTitle.Create(title);
        var tipDescription = TipDescription.Create("Test description for " + title);
        var steps = new[]
        {
            TipStep.Create(1, "First step"),
            TipStep.Create(2, "Second step")
        };
        var tipTags = new[] { Tag.Create("test") };

        return Tip.Create(tipTitle, tipDescription, steps, _testCategory.Id, tipTags);
    }
}
