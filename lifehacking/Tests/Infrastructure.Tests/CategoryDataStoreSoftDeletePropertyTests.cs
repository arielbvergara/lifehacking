using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for FirestoreCategoryDataStore soft delete filtering functionality.
/// Feature: firestore-test-infrastructure-improvements
/// 
/// WHY THIS TEST CLASS EXISTS:
/// Category did NOT have soft delete support before this spec. This test class validates the
/// NEW soft delete functionality added to CategoryDataStore, including:
/// - Soft delete fields persist correctly to Firestore
/// - GetByIdAsync filters out soft-deleted categories
/// - Collection queries (GetAllAsync, GetByNameAsync) exclude soft-deleted categories
/// - GetAllIncludingDeletedAsync retrieves all categories regardless of deletion status
/// 
/// Uses property-based testing to verify these behaviors hold across a wide range of
/// random inputs (100 iterations per test), providing stronger correctness guarantees
/// than example-based tests alone.
/// 
/// RELATIONSHIP TO OTHER TESTS:
/// - CategoryRepositoryTests: Tests the Repository layer with example-based tests
/// - CategorySoftDeletePropertyTests: Tests the Category entity's MarkDeleted behavior
/// - This class: Tests the DataStore layer's soft delete filtering with property-based tests
/// 
/// Tests Properties 4, 6, 7, and 9 related to data store operations.
/// </summary>
public sealed class CategoryDataStoreSoftDeletePropertyTests : FirestoreTestBase
{
    private readonly FirestoreCategoryDataStore _categoryDataStore;

    public CategoryDataStoreSoftDeletePropertyTests()
    {
        _categoryDataStore = new FirestoreCategoryDataStore(FirestoreDb, CollectionNameProvider);
    }

    // Feature: firestore-test-infrastructure-improvements, Property 4: Soft Delete Persistence Round-Trip
    // For any Tip or Category entity with specific IsDeleted and DeletedAt values, persisting the entity 
    // to Firestore and then retrieving it should preserve those exact values.
    // Validates: Requirements 3.6, 3.7, 4.4, 4.5, 4.6, 5.2

    /// <summary>
    /// Property: Persisting a category with soft delete values and retrieving it should preserve those values.
    /// This property verifies that soft delete fields are correctly persisted and retrieved.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task SoftDeleteFields_ShouldBePreserved_WhenPersistingAndRetrieving(
        NonEmptyString name,
        bool isDeleted)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length <= 1)
        {
            return; // Skip invalid inputs - Category requires at least 2 characters after trimming
        }

        // Arrange: Create a category
        var category = Category.Create(nameStr);

        // Set soft delete state if needed
        if (isDeleted)
        {
            category.MarkDeleted();
        }

        var originalIsDeleted = category.IsDeleted;
        var originalDeletedAt = category.DeletedAt;

        // Act: Persist the category
        await _categoryDataStore.AddAsync(category);

        // Retrieve using GetAllIncludingDeletedAsync to bypass filtering
        var allCategories = await _categoryDataStore.GetAllIncludingDeletedAsync();
        var retrievedCategory = allCategories.FirstOrDefault(c => c.Id == category.Id);

        // Assert: Soft delete fields should be preserved
        retrievedCategory.Should().NotBeNull("the category should be retrievable");
        retrievedCategory!.IsDeleted.Should().Be(originalIsDeleted, "IsDeleted should be preserved");

        if (originalDeletedAt.HasValue)
        {
            retrievedCategory.DeletedAt.Should().NotBeNull("DeletedAt should be preserved when set");
            retrievedCategory.DeletedAt!.Value.Should().BeCloseTo(
                originalDeletedAt.Value,
                TimeSpan.FromSeconds(1),
                "DeletedAt should match within 1 second");
        }
        else
        {
            retrievedCategory.DeletedAt.Should().BeNull("DeletedAt should be null when not set");
        }
    }

    // Feature: firestore-test-infrastructure-improvements, Property 6: GetById Filters Soft-Deleted Entities
    // For any soft-deleted Tip or Category entity (IsDeleted = true), calling GetByIdAsync on the 
    // respective repository should return null, even though the entity exists in the data store.
    // Validates: Requirements 4.4

    /// <summary>
    /// Property: GetByIdAsync should return null for soft-deleted categories.
    /// This property verifies that soft-deleted entities are filtered out by GetByIdAsync.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryIsSoftDeleted(
        NonEmptyString name)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length <= 1)
        {
            return; // Skip invalid inputs - Category requires at least 2 characters after trimming
        }

        // Arrange: Create and persist a category
        var category = Category.Create(nameStr);
        await _categoryDataStore.AddAsync(category);

        // Mark as deleted and update
        category.MarkDeleted();
        await _categoryDataStore.UpdateAsync(category);

        // Act: Try to retrieve the soft-deleted category
        var retrievedCategory = await _categoryDataStore.GetByIdAsync(category.Id);

        // Assert: Should return null because the category is soft-deleted
        retrievedCategory.Should().BeNull("GetByIdAsync should filter out soft-deleted categories");

        // Verify the category still exists in the data store
        var allCategories = await _categoryDataStore.GetAllIncludingDeletedAsync();
        var existsInDataStore = allCategories.Any(c => c.Id == category.Id);

        existsInDataStore.Should().BeTrue("the category should still exist in the data store");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 7: Collection Queries Exclude Soft-Deleted Entities
    // For any query operation that returns multiple entities (SearchAsync, GetByCategoryAsync, GetAllAsync, 
    // GetByNameAsync), the results should exclude all entities where IsDeleted = true.
    // Validates: Requirements 4.5, 4.6

    /// <summary>
    /// Property: GetAllAsync should exclude soft-deleted categories from results.
    /// This property verifies that collection queries filter out soft-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetAllAsync_ShouldExcludeSoftDeletedCategories_WhenQuerying(
        PositiveInt deletedCount,
        PositiveInt activeCount)
    {
        // Arrange: Create some deleted categories (1-5)
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);
        var deletedCategoryIds = new List<CategoryId>();
        for (int i = 0; i < actualDeletedCount; i++)
        {
            var category = Category.Create($"Deleted Category {i} {Guid.NewGuid():N}");
            category.MarkDeleted();
            await _categoryDataStore.AddAsync(category);
            deletedCategoryIds.Add(category.Id);
        }

        // Create some active categories (1-5)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        var activeCategoryIds = new List<CategoryId>();
        for (int i = 0; i < actualActiveCount; i++)
        {
            var category = Category.Create($"Active Category {i} {Guid.NewGuid():N}");
            await _categoryDataStore.AddAsync(category);
            activeCategoryIds.Add(category.Id);
        }

        // Act: Get all categories
        var categories = await _categoryDataStore.GetAllAsync();

        // Filter to only categories from this test (by checking if they're in our ID lists)
        var testCategories = categories.Where(c =>
            deletedCategoryIds.Contains(c.Id) || activeCategoryIds.Contains(c.Id)).ToList();

        // Assert: Should only return active categories
        testCategories.Should().HaveCount(actualActiveCount, "only active categories should be returned");
        testCategories.Should().OnlyContain(c => !c.IsDeleted, "all returned categories should have IsDeleted = false");
        testCategories.Select(c => c.Id).Should().Contain(activeCategoryIds, "should include all active category IDs");
        testCategories.Select(c => c.Id).Should().NotContain(deletedCategoryIds, "should not include deleted category IDs");
    }

    /// <summary>
    /// Property: GetByNameAsync should return null for soft-deleted categories.
    /// This property verifies that name queries filter out soft-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetByNameAsync_ShouldReturnNull_WhenCategoryIsSoftDeleted(
        NonEmptyString name)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length <= 1)
        {
            return; // Skip invalid inputs - Category requires at least 2 characters after trimming
        }

        // Arrange: Create and persist a category
        var category = Category.Create(nameStr);
        await _categoryDataStore.AddAsync(category);

        // Mark as deleted and update
        category.MarkDeleted();
        await _categoryDataStore.UpdateAsync(category);

        // Act: Try to retrieve the soft-deleted category by name
        var retrievedCategory = await _categoryDataStore.GetByNameAsync(nameStr);

        // Assert: Should return null because the category is soft-deleted
        retrievedCategory.Should().BeNull("GetByNameAsync should filter out soft-deleted categories");

        // Verify the category still exists in the data store
        var allCategories = await _categoryDataStore.GetAllIncludingDeletedAsync();
        var existsInDataStore = allCategories.Any(c => c.Id == category.Id);

        existsInDataStore.Should().BeTrue("the category should still exist in the data store");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 9: Explicit Deleted Entity Retrieval
    // For any collection containing both deleted and non-deleted entities, calling GetAllIncludingDeletedAsync 
    // should return all entities regardless of their IsDeleted status, while standard query methods should 
    // only return non-deleted entities.
    // Validates: Requirements 5.2

    /// <summary>
    /// Property: GetAllIncludingDeletedAsync should return all categories regardless of IsDeleted status.
    /// This property verifies that explicit deleted entity retrieval works correctly.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetAllIncludingDeletedAsync_ShouldReturnAllCategories_RegardlessOfDeletedStatus(
        PositiveInt deletedCount,
        PositiveInt activeCount)
    {
        // Arrange: Create some deleted categories (1-5)
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);
        var deletedCategoryIds = new List<CategoryId>();
        for (int i = 0; i < actualDeletedCount; i++)
        {
            var category = Category.Create($"Deleted Category {i} {Guid.NewGuid():N}");
            category.MarkDeleted();
            await _categoryDataStore.AddAsync(category);
            deletedCategoryIds.Add(category.Id);
        }

        // Create some active categories (1-5)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        var activeCategoryIds = new List<CategoryId>();
        for (int i = 0; i < actualActiveCount; i++)
        {
            var category = Category.Create($"Active Category {i} {Guid.NewGuid():N}");
            await _categoryDataStore.AddAsync(category);
            activeCategoryIds.Add(category.Id);
        }

        // Act: Get all categories including deleted
        var allCategories = await _categoryDataStore.GetAllIncludingDeletedAsync();

        // Filter to only categories from this test (by checking if they're in our ID lists)
        var testCategories = allCategories.Where(c =>
            deletedCategoryIds.Contains(c.Id) || activeCategoryIds.Contains(c.Id)).ToList();

        // Assert: Should return both deleted and active categories
        var totalExpected = actualDeletedCount + actualActiveCount;
        testCategories.Should().HaveCount(totalExpected, "should return all categories regardless of IsDeleted status");

        var deletedCategories = testCategories.Where(c => c.IsDeleted).ToList();
        var activeCategories = testCategories.Where(c => !c.IsDeleted).ToList();

        deletedCategories.Should().HaveCount(actualDeletedCount, "should include all deleted categories");
        activeCategories.Should().HaveCount(actualActiveCount, "should include all active categories");

        // Verify that standard query methods exclude deleted categories
        var standardResults = await _categoryDataStore.GetAllAsync();
        var standardResultIds = standardResults.Select(c => c.Id).ToList();

        // Standard GetAllAsync should only return active categories
        standardResultIds.Should().Contain(activeCategoryIds, "standard GetAllAsync should return active categories");
        standardResultIds.Should().NotContain(deletedCategoryIds, "standard GetAllAsync should not return deleted categories");
    }
}
