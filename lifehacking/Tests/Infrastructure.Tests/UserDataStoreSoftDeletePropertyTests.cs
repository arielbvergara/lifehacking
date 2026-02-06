using Application.Dtos;
using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Data.Firestore;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for FirestoreUserDataStore soft delete filtering functionality.
/// Feature: firestore-test-infrastructure-improvements
/// 
/// WHY THIS TEST CLASS EXISTS:
/// User already had soft delete support before this spec. This test class validates that
/// adding collection namespacing (ICollectionNameProvider) didn't break the existing
/// soft delete behavior. It uses property-based testing to verify the behavior holds
/// across a wide range of random inputs (100 iterations per test).
/// 
/// RELATIONSHIP TO OTHER TESTS:
/// - UserRepositoryTests: Tests the Repository layer with example-based tests
/// - This class: Tests the DataStore layer with property-based tests
/// Both are needed because they test different layers and use different testing approaches.
/// 
/// Tests Property 8: User Repository Maintains Existing Soft Delete Behavior
/// </summary>
public sealed class UserDataStoreSoftDeletePropertyTests : FirestoreTestBase
{
    private readonly FirestoreUserDataStore _userDataStore;

    public UserDataStoreSoftDeletePropertyTests()
    {
        _userDataStore = new FirestoreUserDataStore(FirestoreDb, CollectionNameProvider);
    }

    // Feature: firestore-test-infrastructure-improvements, Property 8: User Repository Maintains Existing Soft Delete Behavior
    // For any user query operation, the UserRepository should continue to filter soft-deleted users 
    // according to the IsDeletedFilter parameter, maintaining backward compatibility with existing behavior.
    // Validates: Requirements 4.8, 5.4

    /// <summary>
    /// Property: GetByIdAsync should return null for soft-deleted users.
    /// This property verifies that GetByIdAsync filters out soft-deleted users.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserIsSoftDeleted(PositiveInt seed)
    {
        // Arrange: Create and persist a user with a unique email
        var uniqueId = $"{seed.Get}_{Guid.NewGuid():N}";
        var userEmail = Email.Create($"user{uniqueId}@example.com");
        var userName = UserName.Create($"User {uniqueId}");
        var externalAuthId = ExternalAuthIdentifier.Create($"auth_{uniqueId}");

        var user = User.Create(userEmail, userName, externalAuthId);
        await _userDataStore.AddAsync(user);

        // Mark as deleted and update
        user.MarkDeleted();
        await _userDataStore.UpdateAsync(user);

        // Act: Try to retrieve the soft-deleted user
        var retrievedUser = await _userDataStore.GetByIdAsync(user.Id);

        // Assert: Should return null because the user is soft-deleted
        retrievedUser.Should().BeNull("GetByIdAsync should filter out soft-deleted users");
    }

    /// <summary>
    /// Property: GetByEmailAsync should return null for soft-deleted users.
    /// This property verifies that GetByEmailAsync filters out soft-deleted users.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserIsSoftDeleted(PositiveInt seed)
    {
        // Arrange: Create and persist a user with a unique email
        var uniqueId = $"{seed.Get}_{Guid.NewGuid():N}";
        var userEmail = Email.Create($"user{uniqueId}@example.com");
        var userName = UserName.Create($"User {uniqueId}");
        var externalAuthId = ExternalAuthIdentifier.Create($"auth_{uniqueId}");

        var user = User.Create(userEmail, userName, externalAuthId);
        await _userDataStore.AddAsync(user);

        // Mark as deleted and update
        user.MarkDeleted();
        await _userDataStore.UpdateAsync(user);

        // Act: Try to retrieve the soft-deleted user by email
        var retrievedUser = await _userDataStore.GetByEmailAsync(userEmail);

        // Assert: Should return null because the user is soft-deleted
        retrievedUser.Should().BeNull("GetByEmailAsync should filter out soft-deleted users");
    }

    /// <summary>
    /// Property: GetByExternalAuthIdAsync should return null for soft-deleted users.
    /// This property verifies that GetByExternalAuthIdAsync filters out soft-deleted users.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task GetByExternalAuthIdAsync_ShouldReturnNull_WhenUserIsSoftDeleted(PositiveInt seed)
    {
        // Arrange: Create and persist a user with a unique email
        var uniqueId = $"{seed.Get}_{Guid.NewGuid():N}";
        var userEmail = Email.Create($"user{uniqueId}@example.com");
        var userName = UserName.Create($"User {uniqueId}");
        var externalAuthId = ExternalAuthIdentifier.Create($"auth_{uniqueId}");

        var user = User.Create(userEmail, userName, externalAuthId);
        await _userDataStore.AddAsync(user);

        // Mark as deleted and update
        user.MarkDeleted();
        await _userDataStore.UpdateAsync(user);

        // Act: Try to retrieve the soft-deleted user by external auth ID
        var retrievedUser = await _userDataStore.GetByExternalAuthIdAsync(externalAuthId);

        // Assert: Should return null because the user is soft-deleted
        retrievedUser.Should().BeNull("GetByExternalAuthIdAsync should filter out soft-deleted users");
    }

    /// <summary>
    /// Property: GetPagedAsync should respect IsDeletedFilter parameter.
    /// This property verifies that GetPagedAsync maintains existing behavior with IsDeletedFilter.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task GetPagedAsync_ShouldRespectIsDeletedFilter_WhenQuerying(PositiveInt seed)
    {
        // Arrange: Create a unique test ID for this property test run
        var testId = $"{seed.Get}_{Guid.NewGuid():N}";

        // Create one deleted user
        var deletedEmail = Email.Create($"deleted_{testId}@example.com");
        var deletedName = UserName.Create($"Deleted User {testId}");
        var deletedAuthId = ExternalAuthIdentifier.Create($"auth_deleted_{testId}");

        var deletedUser = User.Create(deletedEmail, deletedName, deletedAuthId);
        deletedUser.MarkDeleted();
        await _userDataStore.AddAsync(deletedUser);

        // Create one active user
        var activeEmail = Email.Create($"active_{testId}@example.com");
        var activeName = UserName.Create($"Active User {testId}");
        var activeAuthId = ExternalAuthIdentifier.Create($"auth_active_{testId}");

        var activeUser = User.Create(activeEmail, activeName, activeAuthId);
        await _userDataStore.AddAsync(activeUser);

        // Act & Assert: Test with IsDeletedFilter = null (should return all users including these two)
        var criteriaNoFilter = new UserQueryCriteria(
            SearchTerm: null,
            IsDeletedFilter: null,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (noFilterItems, _) = await _userDataStore.GetPagedAsync(criteriaNoFilter);

        // When IsDeletedFilter is null, should return all users (both active and deleted)
        // Note: May contain users from other property test iterations in the same test class instance
        noFilterItems.Should().Contain(u => u.Id == deletedUser.Id, "should include deleted user");
        noFilterItems.Should().Contain(u => u.Id == activeUser.Id, "should include active user");

        // Act & Assert: Test with IsDeletedFilter = false (should return only active users)
        var criteriaActiveOnly = new UserQueryCriteria(
            SearchTerm: null,
            IsDeletedFilter: false,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (activeItems, _) = await _userDataStore.GetPagedAsync(criteriaActiveOnly);

        activeItems.Should().Contain(u => u.Id == activeUser.Id, "should contain the active user");
        activeItems.Should().NotContain(u => u.Id == deletedUser.Id, "should not contain the deleted user");
        activeItems.Should().OnlyContain(u => !u.IsDeleted, "all returned users should have IsDeleted = false");

        // Act & Assert: Test with IsDeletedFilter = true (should return only deleted users)
        var criteriaDeletedOnly = new UserQueryCriteria(
            SearchTerm: null,
            IsDeletedFilter: true,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (deletedItems, _) = await _userDataStore.GetPagedAsync(criteriaDeletedOnly);

        deletedItems.Should().Contain(u => u.Id == deletedUser.Id, "should contain the deleted user");
        deletedItems.Should().NotContain(u => u.Id == activeUser.Id, "should not contain the active user");
        deletedItems.Should().OnlyContain(u => u.IsDeleted, "all returned users should have IsDeleted = true");
    }

    /// <summary>
    /// Property: Soft delete fields should be preserved when persisting and retrieving users.
    /// This property verifies that soft delete fields are correctly persisted and retrieved.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task SoftDeleteFields_ShouldBePreserved_WhenPersistingAndRetrieving(
        PositiveInt seed,
        bool isDeleted)
    {
        // Arrange: Create a user with a unique email
        var uniqueId = $"{seed.Get}_{Guid.NewGuid():N}";
        var userEmail = Email.Create($"user{uniqueId}@example.com");
        var userName = UserName.Create($"User {uniqueId}");
        var externalAuthId = ExternalAuthIdentifier.Create($"auth_{uniqueId}");

        var user = User.Create(userEmail, userName, externalAuthId);

        // Set soft delete state if needed
        if (isDeleted)
        {
            user.MarkDeleted();
        }

        var originalIsDeleted = user.IsDeleted;
        var originalDeletedAt = user.DeletedAt;

        // Act: Persist the user
        await _userDataStore.AddAsync(user);

        // Retrieve using GetPagedAsync with IsDeletedFilter to include deleted users
        var criteria = new UserQueryCriteria(
            SearchTerm: null,
            IsDeletedFilter: isDeleted ? true : false,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Ascending,
            PageNumber: 1,
            PageSize: 100
        );

        var (items, _) = await _userDataStore.GetPagedAsync(criteria);
        var retrievedUser = items.FirstOrDefault(u => u.Id == user.Id);

        // Assert: Soft delete fields should be preserved
        retrievedUser.Should().NotBeNull("the user should be retrievable");
        retrievedUser!.IsDeleted.Should().Be(originalIsDeleted, "IsDeleted should be preserved");

        if (originalDeletedAt.HasValue)
        {
            retrievedUser.DeletedAt.Should().NotBeNull("DeletedAt should be preserved when set");
            retrievedUser.DeletedAt!.Value.Should().BeCloseTo(
                originalDeletedAt.Value,
                TimeSpan.FromSeconds(1),
                "DeletedAt should match within 1 second");
        }
        else
        {
            retrievedUser.DeletedAt.Should().BeNull("DeletedAt should be null when not set");
        }
    }
}
