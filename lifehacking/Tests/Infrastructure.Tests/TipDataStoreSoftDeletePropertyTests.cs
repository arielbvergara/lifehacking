using Application.Dtos;
using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for FirestoreTipDataStore soft delete filtering functionality.
/// Feature: firestore-test-infrastructure-improvements
/// 
/// WHY THIS TEST CLASS EXISTS:
/// Tip did NOT have soft delete support before this spec. This test class validates the
/// NEW soft delete functionality added to TipDataStore, including:
/// - Soft delete fields persist correctly to Firestore
/// - GetByIdAsync filters out soft-deleted tips
/// - Collection queries (SearchAsync, GetByCategoryAsync) exclude soft-deleted tips
/// - GetAllIncludingDeletedAsync retrieves all tips regardless of deletion status
/// 
/// Uses property-based testing to verify these behaviors hold across a wide range of
/// random inputs (100 iterations per test), providing stronger correctness guarantees
/// than example-based tests alone.
/// 
/// RELATIONSHIP TO OTHER TESTS:
/// - TipRepositoryTests: Tests the Repository layer with example-based tests
/// - TipSoftDeletePropertyTests: Tests the Tip entity's MarkDeleted behavior
/// - This class: Tests the DataStore layer's soft delete filtering with property-based tests
/// 
/// Tests Properties 4, 6, 7, and 9 related to data store operations.
/// </summary>
public sealed class TipDataStoreSoftDeletePropertyTests : FirestoreTestBase
{
    private readonly FirestoreTipDataStore _tipDataStore;

    public TipDataStoreSoftDeletePropertyTests()
    {
        _tipDataStore = new FirestoreTipDataStore(FirestoreDb, CollectionNameProvider);
    }
    // Feature: firestore-test-infrastructure-improvements, Property 4: Soft Delete Persistence Round-Trip
    // For any Tip or Category entity with specific IsDeleted and DeletedAt values, persisting the entity 
    // to Firestore and then retrieving it should preserve those exact values.
    // Validates: Requirements 2.6, 2.7, 4.1, 4.2, 4.3, 5.1

    /// <summary>
    /// Property: Persisting a tip with soft delete values and retrieving it should preserve those values.
    /// This property verifies that soft delete fields are correctly persisted and retrieved.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task SoftDeleteFields_ShouldBePreserved_WhenPersistingAndRetrieving(
        NonEmptyString title,
        NonEmptyString description,
        bool isDeleted)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        // Note: We check the trimmed length because TipTitle validates before trimming
        // but stores the trimmed value, which can cause validation failures on retrieval
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Trim().Length < 5 || descriptionStr.Trim().Length < 11)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a tip with a random category ID
        var categoryId = CategoryId.NewId();
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var steps = new[] { TipStep.Create(1, "Test step description") };

        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);

        // Set soft delete state if needed
        if (isDeleted)
        {
            tip.MarkDeleted();
        }

        var originalIsDeleted = tip.IsDeleted;
        var originalDeletedAt = tip.DeletedAt;

        // Act: Persist the tip
        await _tipDataStore.AddAsync(tip);

        // Retrieve using GetAllIncludingDeletedAsync to bypass filtering
        var allTips = await _tipDataStore.GetAllIncludingDeletedAsync();
        var retrievedTip = allTips.FirstOrDefault(t => t.Id == tip.Id);

        // Assert: Soft delete fields should be preserved
        retrievedTip.Should().NotBeNull("the tip should be retrievable");
        retrievedTip!.IsDeleted.Should().Be(originalIsDeleted, "IsDeleted should be preserved");

        if (originalDeletedAt.HasValue)
        {
            retrievedTip.DeletedAt.Should().NotBeNull("DeletedAt should be preserved when set");
            retrievedTip.DeletedAt!.Value.Should().BeCloseTo(
                originalDeletedAt.Value,
                TimeSpan.FromSeconds(1),
                "DeletedAt should match within 1 second");
        }
        else
        {
            retrievedTip.DeletedAt.Should().BeNull("DeletedAt should be null when not set");
        }
    }

    // Feature: firestore-test-infrastructure-improvements, Property 6: GetById Filters Soft-Deleted Entities
    // For any soft-deleted Tip or Category entity (IsDeleted = true), calling GetByIdAsync on the 
    // respective repository should return null, even though the entity exists in the data store.
    // Validates: Requirements 4.1, 4.4

    /// <summary>
    /// Property: GetByIdAsync should return null for soft-deleted tips.
    /// This property verifies that soft-deleted entities are filtered out by GetByIdAsync.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTipIsSoftDeleted(
        NonEmptyString title,
        NonEmptyString description)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        // Note: We check the trimmed length because TipTitle validates before trimming
        // but stores the trimmed value, which can cause validation failures on retrieval
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Trim().Length < 5 || descriptionStr.Trim().Length < 11)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create and persist a tip
        var categoryId = CategoryId.NewId();
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var steps = new[] { TipStep.Create(1, "Test step description") };

        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);
        await _tipDataStore.AddAsync(tip);

        // Mark as deleted and update
        tip.MarkDeleted();
        await _tipDataStore.UpdateAsync(tip);

        // Act: Try to retrieve the soft-deleted tip
        var retrievedTip = await _tipDataStore.GetByIdAsync(tip.Id);

        // Assert: Should return null because the tip is soft-deleted
        retrievedTip.Should().BeNull("GetByIdAsync should filter out soft-deleted tips");

        // Verify the tip still exists in the data store
        var allTips = await _tipDataStore.GetAllIncludingDeletedAsync();
        var existsInDataStore = allTips.Any(t => t.Id == tip.Id);

        existsInDataStore.Should().BeTrue("the tip should still exist in the data store");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 7: Collection Queries Exclude Soft-Deleted Entities
    // For any query operation that returns multiple entities (SearchAsync, GetByCategoryAsync, GetAllAsync, 
    // GetByNameAsync), the results should exclude all entities where IsDeleted = true.
    // Validates: Requirements 4.2, 4.3, 4.5, 4.6

    /// <summary>
    /// Property: SearchAsync should exclude soft-deleted tips from results.
    /// This property verifies that collection queries filter out soft-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task SearchAsync_ShouldExcludeSoftDeletedTips_WhenQuerying(
        NonEmptyString searchTerm,
        PositiveInt deletedCount,
        PositiveInt activeCount)
    {
        var searchTermStr = searchTerm.Get;

        if (searchTermStr.Length < 3)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a category ID
        var categoryId = CategoryId.NewId();

        // Create some deleted tips (1-5)
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);
        for (int i = 0; i < actualDeletedCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"{searchTermStr} Deleted Tip {i}"),
                TipDescription.Create($"Description for deleted tip {i} with search term {searchTermStr}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            tip.MarkDeleted();
            await _tipDataStore.AddAsync(tip);
        }

        // Create some active tips (1-5)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        for (int i = 0; i < actualActiveCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"{searchTermStr} Active Tip {i}"),
                TipDescription.Create($"Description for active tip {i} with search term {searchTermStr}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            await _tipDataStore.AddAsync(tip);
        }

        // Act: Search for tips with the search term
        var criteria = new TipQueryCriteria(
            SearchTerm: searchTermStr,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (items, totalCount) = await _tipDataStore.SearchAsync(criteria);

        // Assert: Should only return active tips
        items.Should().HaveCount(actualActiveCount, "only active tips should be returned");
        items.Should().OnlyContain(t => !t.IsDeleted, "all returned tips should have IsDeleted = false");
        totalCount.Should().Be(actualActiveCount, "total count should only include active tips");
    }

    /// <summary>
    /// Property: GetByCategoryAsync should exclude soft-deleted tips from results.
    /// This property verifies that category queries filter out soft-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetByCategoryAsync_ShouldExcludeSoftDeletedTips_WhenQuerying(
        PositiveInt deletedCount,
        PositiveInt activeCount)
    {
        // Arrange: Create a category ID
        var categoryId = CategoryId.NewId();

        // Create some deleted tips (1-5)
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);
        for (int i = 0; i < actualDeletedCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"Deleted Tip {i} {Guid.NewGuid():N}"),
                TipDescription.Create($"Description for deleted tip {i}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            tip.MarkDeleted();
            await _tipDataStore.AddAsync(tip);
        }

        // Create some active tips (1-5)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        for (int i = 0; i < actualActiveCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"Active Tip {i} {Guid.NewGuid():N}"),
                TipDescription.Create($"Description for active tip {i}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            await _tipDataStore.AddAsync(tip);
        }

        // Act: Get tips by category
        var tips = await _tipDataStore.GetByCategoryAsync(categoryId);

        // Assert: Should only return active tips
        tips.Should().HaveCount(actualActiveCount, "only active tips should be returned");
        tips.Should().OnlyContain(t => !t.IsDeleted, "all returned tips should have IsDeleted = false");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 9: Explicit Deleted Entity Retrieval
    // For any collection containing both deleted and non-deleted entities, calling GetAllIncludingDeletedAsync 
    // should return all entities regardless of their IsDeleted status, while standard query methods should 
    // only return non-deleted entities.
    // Validates: Requirements 5.1, 5.2

    /// <summary>
    /// Property: GetAllIncludingDeletedAsync should return all tips regardless of IsDeleted status.
    /// This property verifies that explicit deleted entity retrieval works correctly.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetAllIncludingDeletedAsync_ShouldReturnAllTips_RegardlessOfDeletedStatus(
        PositiveInt deletedCount,
        PositiveInt activeCount)
    {
        // Arrange: Create a category ID
        var categoryId = CategoryId.NewId();

        // Create some deleted tips (1-5)
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);
        var deletedTipIds = new List<TipId>();
        for (int i = 0; i < actualDeletedCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"Deleted Tip {i} {Guid.NewGuid():N}"),
                TipDescription.Create($"Description for deleted tip {i}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            tip.MarkDeleted();
            await _tipDataStore.AddAsync(tip);
            deletedTipIds.Add(tip.Id);
        }

        // Create some active tips (1-5)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        var activeTipIds = new List<TipId>();
        for (int i = 0; i < actualActiveCount; i++)
        {
            var tip = Tip.Create(
                TipTitle.Create($"Active Tip {i} {Guid.NewGuid():N}"),
                TipDescription.Create($"Description for active tip {i}"),
                new[] { TipStep.Create(1, "Test step description") },
                categoryId);

            await _tipDataStore.AddAsync(tip);
            activeTipIds.Add(tip.Id);
        }

        // Act: Get all tips including deleted
        var allTips = await _tipDataStore.GetAllIncludingDeletedAsync();

        // Filter to only tips from this test (by checking if they're in our ID lists)
        var testTips = allTips.Where(t =>
            deletedTipIds.Contains(t.Id) || activeTipIds.Contains(t.Id)).ToList();

        // Assert: Should return both deleted and active tips
        var totalExpected = actualDeletedCount + actualActiveCount;
        testTips.Should().HaveCount(totalExpected, "should return all tips regardless of IsDeleted status");

        var deletedTips = testTips.Where(t => t.IsDeleted).ToList();
        var activeTips = testTips.Where(t => !t.IsDeleted).ToList();

        deletedTips.Should().HaveCount(actualDeletedCount, "should include all deleted tips");
        activeTips.Should().HaveCount(actualActiveCount, "should include all active tips");

        // Verify that standard query methods exclude deleted tips
        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: categoryId.Value,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (searchResults, _) = await _tipDataStore.SearchAsync(criteria);
        var searchResultIds = searchResults.Select(t => t.Id).ToList();

        // Standard search should only return active tips
        searchResultIds.Should().Contain(activeTipIds, "standard search should return active tips");
        searchResultIds.Should().NotContain(deletedTipIds, "standard search should not return deleted tips");
    }
}
