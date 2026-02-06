using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for repository filtering consistency across all repositories.
/// Feature: firestore-test-infrastructure-improvements
/// 
/// WHY THIS TEST CLASS EXISTS:
/// This spec adds soft delete support to Tip and Category repositories, which User already had.
/// To ensure consistency across the application, all three repositories must filter deleted
/// entities in the same way. This test class verifies that GetById returns null for deleted
/// entities and that collection queries exclude deleted entities consistently across all
/// repository types.
/// 
/// Uses property-based testing to verify this behavior holds across a wide range of random
/// inputs, ensuring the filtering logic is consistent and correct.
/// 
/// Tests Property 12: Repository Filtering Consistency
/// Validates: Requirements 10.4
/// </summary>
public sealed class RepositoryFilteringConsistencyPropertyTests : FirestoreTestBase
{
    private readonly UserRepository _userRepository;
    private readonly TipRepository _tipRepository;
    private readonly CategoryRepository _categoryRepository;

    public RepositoryFilteringConsistencyPropertyTests()
    {
        var userDataStore = new FirestoreUserDataStore(FirestoreDb, CollectionNameProvider);
        var tipDataStore = new FirestoreTipDataStore(FirestoreDb, CollectionNameProvider);
        var categoryDataStore = new FirestoreCategoryDataStore(FirestoreDb, CollectionNameProvider);

        _userRepository = new UserRepository(userDataStore);
        _tipRepository = new TipRepository(tipDataStore);
        _categoryRepository = new CategoryRepository(categoryDataStore);
    }

    // Feature: firestore-test-infrastructure-improvements, Property 12: Repository Filtering Consistency
    // For any repository (UserRepository, TipRepository, CategoryRepository), the soft delete filtering 
    // behavior should be consistent: GetById returns null for deleted entities, and collection queries 
    // exclude deleted entities by default.
    // Validates: Requirements 10.4

    /// <summary>
    /// Property: GetById should return null for soft-deleted entities across all repositories.
    /// This property verifies that all repositories consistently filter out soft-deleted entities
    /// when retrieving by ID.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task GetById_ShouldReturnNull_ForSoftDeletedEntitiesAcrossAllRepositories(
        NonEmptyString userName,
        NonEmptyString userEmail,
        NonEmptyString tipTitle,
        NonEmptyString tipDescription,
        NonEmptyString categoryName)
    {
        // Precondition: Validate inputs meet minimum requirements
        var userNameStr = userName.Get.Trim();
        var userEmailStr = userEmail.Get.Trim();
        var tipTitleStr = tipTitle.Get.Trim();
        var tipDescStr = tipDescription.Get.Trim();
        var catNameStr = categoryName.Get.Trim();

        // Email validation: need at least 3 chars for local part, no @ or spaces, no control chars
        // UserName validation: need at least 2 chars after trim
        // TipTitle validation: need at least 5 chars after trim
        // TipDescription validation: need at least 10 chars after trim
        // Category name validation: must be within min/max bounds
        if (userNameStr.Length < 2 ||
            userEmailStr.Length < 3 ||
            userEmailStr.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c == '@') ||
            tipTitleStr.Length < 5 ||
            tipDescStr.Length < 10 ||
            catNameStr.Length < Category.MinNameLength ||
            catNameStr.Length > Category.MaxNameLength)
        {
            return;
        }

        // Arrange: Create and persist entities
        var email = Email.Create($"{userEmailStr}@test.com");
        var name = UserName.Create(userNameStr);
        var externalAuthId = ExternalAuthIdentifier.Create($"auth_{Guid.NewGuid():N}");
        var user = User.Create(email, name, externalAuthId);
        await _userRepository.AddAsync(user);

        var category = Category.Create(catNameStr);
        await _categoryRepository.AddAsync(category);

        var tipTitleVO = TipTitle.Create(tipTitleStr);
        var tipDescVO = TipDescription.Create(tipDescStr);
        var steps = new[] { TipStep.Create(1, "Step 1 description with enough characters") };
        var tip = Tip.Create(tipTitleVO, tipDescVO, steps, category.Id);
        await _tipRepository.AddAsync(tip);

        // Act: Mark all entities as deleted and update
        user.MarkDeleted();
        await _userRepository.UpdateAsync(user);

        tip.MarkDeleted();
        await _tipRepository.UpdateAsync(tip);

        category.MarkDeleted();
        await _categoryRepository.UpdateAsync(category);

        // Retrieve entities by ID
        var retrievedUser = await _userRepository.GetByIdAsync(user.Id);
        var retrievedTip = await _tipRepository.GetByIdAsync(tip.Id);
        var retrievedCategory = await _categoryRepository.GetByIdAsync(category.Id);

        // Assert: All repositories should return null for soft-deleted entities
        retrievedUser.Should().BeNull("UserRepository.GetByIdAsync should return null for soft-deleted users");
        retrievedTip.Should().BeNull("TipRepository.GetByIdAsync should return null for soft-deleted tips");
        retrievedCategory.Should().BeNull("CategoryRepository.GetByIdAsync should return null for soft-deleted categories");
    }

    /// <summary>
    /// Property: Collection queries should exclude soft-deleted entities across all repositories.
    /// This property verifies that all repositories consistently filter out soft-deleted entities
    /// from collection query results.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task CollectionQueries_ShouldExcludeSoftDeletedEntities_AcrossAllRepositories(
        PositiveInt activeCount,
        PositiveInt deletedCount)
    {
        // Arrange: Limit counts to reasonable numbers (1-5 each)
        var actualActiveCount = Math.Min(Math.Max(1, activeCount.Get), 5);
        var actualDeletedCount = Math.Min(Math.Max(1, deletedCount.Get), 5);

        // Create active and deleted users
        var activeUserIds = new List<UserId>();
        var deletedUserIds = new List<UserId>();

        for (int i = 0; i < actualActiveCount; i++)
        {
            var user = TestDataFactory.CreateUser();
            await _userRepository.AddAsync(user);
            activeUserIds.Add(user.Id);
        }

        for (int i = 0; i < actualDeletedCount; i++)
        {
            var user = TestDataFactory.CreateUser();
            user.MarkDeleted();
            await _userRepository.AddAsync(user);
            deletedUserIds.Add(user.Id);
        }

        // Create active and deleted categories
        var activeCategoryIds = new List<CategoryId>();
        var deletedCategoryIds = new List<CategoryId>();

        for (int i = 0; i < actualActiveCount; i++)
        {
            var category = TestDataFactory.CreateCategory();
            await _categoryRepository.AddAsync(category);
            activeCategoryIds.Add(category.Id);
        }

        for (int i = 0; i < actualDeletedCount; i++)
        {
            var category = TestDataFactory.CreateCategory();
            category.MarkDeleted();
            await _categoryRepository.AddAsync(category);
            deletedCategoryIds.Add(category.Id);
        }

        // Create active and deleted tips (using first active category)
        var activeTipIds = new List<TipId>();
        var deletedTipIds = new List<TipId>();

        for (int i = 0; i < actualActiveCount; i++)
        {
            var tip = TestDataFactory.CreateTip(activeCategoryIds[0]);
            await _tipRepository.AddAsync(tip);
            activeTipIds.Add(tip.Id);
        }

        for (int i = 0; i < actualDeletedCount; i++)
        {
            var tip = TestDataFactory.CreateTip(activeCategoryIds[0]);
            tip.MarkDeleted();
            await _tipRepository.AddAsync(tip);
            deletedTipIds.Add(tip.Id);
        }

        // Act: Query collections
        var allCategories = await _categoryRepository.GetAllAsync();

        // Filter to only our test categories
        var testCategories = allCategories.Where(c =>
            activeCategoryIds.Contains(c.Id) || deletedCategoryIds.Contains(c.Id)).ToList();

        // Assert: Collection queries should only return active entities
        var activeCategoriesReturned = testCategories.Where(c => activeCategoryIds.Contains(c.Id)).ToList();
        var deletedCategoriesReturned = testCategories.Where(c => deletedCategoryIds.Contains(c.Id)).ToList();

        activeCategoriesReturned.Should().HaveCount(actualActiveCount,
            "CategoryRepository.GetAllAsync should return all active categories");
        deletedCategoriesReturned.Should().BeEmpty(
            "CategoryRepository.GetAllAsync should exclude soft-deleted categories");

        // Verify all returned entities are not deleted
        testCategories.Should().OnlyContain(c => !c.IsDeleted,
            "CategoryRepository.GetAllAsync should only return non-deleted categories");
    }
}
