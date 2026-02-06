using Domain.Entities;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for Category entity soft delete functionality.
/// Feature: firestore-test-infrastructure-improvements
/// </summary>
public sealed class CategorySoftDeletePropertyTests
{
    // Feature: firestore-test-infrastructure-improvements, Property 2: New Entity Soft Delete Initial State
    // For any newly created Tip or Category entity, the IsDeleted property should be false and the
    // DeletedAt property should be null.
    // Validates: Requirements 3.1, 3.2

    /// <summary>
    /// Property: Newly created Category entities should have IsDeleted = false and DeletedAt = null.
    /// This property verifies the initial soft delete state of new entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public void NewCategory_ShouldHaveIsDeletedFalseAndDeletedAtNull_WhenCreated(
        NonEmptyString name)
    {
        // Precondition: Ensure name meets minimum length requirements (2-100 characters after trimming)
        var nameStr = name.Get.Trim();

        if (nameStr.Length < Category.MinNameLength || nameStr.Length > Category.MaxNameLength)
        {
            return; // Skip invalid inputs
        }

        // Act: Create a new category
        var category = Category.Create(nameStr);

        // Assert: Initial soft delete state should be false/null
        category.IsDeleted.Should().BeFalse("newly created categories should not be marked as deleted");
        category.DeletedAt.Should().BeNull("newly created categories should not have a deletion timestamp");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 3: MarkDeleted Idempotence and Behavior
    // For any User, Tip, or Category entity, calling MarkDeleted should set IsDeleted to true and
    // DeletedAt to a recent UTC timestamp, and calling MarkDeleted multiple times should produce
    // the same result as calling it once (idempotent operation).
    // Validates: Requirements 3.3, 3.4, 10.2

    /// <summary>
    /// Property: Calling MarkDeleted on a Category should set IsDeleted to true and DeletedAt to a recent UTC timestamp.
    /// This property verifies the basic behavior of the MarkDeleted method.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldSetIsDeletedTrueAndDeletedAtToUtcNow_WhenCalled(
        NonEmptyString name)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length < Category.MinNameLength || nameStr.Length > Category.MaxNameLength)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a new category
        var category = Category.Create(nameStr);
        var beforeDelete = DateTime.UtcNow;

        // Act: Mark the category as deleted
        category.MarkDeleted();
        var afterDelete = DateTime.UtcNow;

        // Assert: IsDeleted should be true
        category.IsDeleted.Should().BeTrue("MarkDeleted should set IsDeleted to true");

        // Assert: DeletedAt should be set to a recent UTC timestamp
        category.DeletedAt.Should().NotBeNull("MarkDeleted should set DeletedAt");
        category.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete, "DeletedAt should be after the method was called");
        category.DeletedAt.Value.Should().BeOnOrBefore(afterDelete, "DeletedAt should be before the method completed");
        category.DeletedAt.Value.Kind.Should().Be(DateTimeKind.Utc, "DeletedAt should be in UTC");
    }

    /// <summary>
    /// Property: Calling MarkDeleted multiple times should be idempotent (same result as calling once).
    /// This property verifies that MarkDeleted can be safely called multiple times.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldBeIdempotent_WhenCalledMultipleTimes(
        NonEmptyString name,
        PositiveInt callCount)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length < Category.MinNameLength || nameStr.Length > Category.MaxNameLength)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a new category
        var category = Category.Create(nameStr);

        // Act: Call MarkDeleted once and capture the state
        category.MarkDeleted();
        var firstIsDeleted = category.IsDeleted;
        var firstDeletedAt = category.DeletedAt;

        // Act: Call MarkDeleted multiple additional times (1-10 times)
        var additionalCalls = Math.Min(Math.Max(1, callCount.Get), 10);
        for (int i = 0; i < additionalCalls; i++)
        {
            // Small delay to ensure time would change if the method wasn't idempotent
            Thread.Sleep(1);
            category.MarkDeleted();
        }

        // Assert: State should be unchanged after additional calls
        category.IsDeleted.Should().Be(firstIsDeleted, "IsDeleted should not change on subsequent calls");
        category.DeletedAt.Should().Be(firstDeletedAt, "DeletedAt should not change on subsequent calls (idempotent)");
    }

    /// <summary>
    /// Property: MarkDeleted should have no effect when called on an already deleted category.
    /// This property verifies the early return behavior for already-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldReturnEarly_WhenCategoryIsAlreadyDeleted(
        NonEmptyString name)
    {
        // Precondition: Ensure name meets minimum length requirements after trimming
        var nameStr = name.Get.Trim();

        if (nameStr.Length < Category.MinNameLength || nameStr.Length > Category.MaxNameLength)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create and delete a category
        var category = Category.Create(nameStr);
        category.MarkDeleted();

        var originalDeletedAt = category.DeletedAt;

        // Act: Wait a bit and call MarkDeleted again
        Thread.Sleep(10);
        category.MarkDeleted();

        // Assert: DeletedAt should not have changed
        category.DeletedAt.Should().Be(originalDeletedAt, "DeletedAt should not change when already deleted");
        category.IsDeleted.Should().BeTrue("IsDeleted should remain true");
    }
}
