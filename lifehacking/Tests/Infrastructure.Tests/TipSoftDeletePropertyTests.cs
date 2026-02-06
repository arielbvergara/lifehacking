using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for Tip entity soft delete functionality.
/// Feature: firestore-test-infrastructure-improvements
/// </summary>
public sealed class TipSoftDeletePropertyTests
{
    // Feature: firestore-test-infrastructure-improvements, Property 2: New Entity Soft Delete Initial State
    // For any newly created Tip or Category entity, the IsDeleted property should be false and the
    // DeletedAt property should be null.
    // Validates: Requirements 2.1, 2.2, 3.1, 3.2

    /// <summary>
    /// Property: Newly created Tip entities should have IsDeleted = false and DeletedAt = null.
    /// This property verifies the initial soft delete state of new entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public void NewTip_ShouldHaveIsDeletedFalseAndDeletedAtNull_WhenCreated(
        NonEmptyString title,
        NonEmptyString description,
        PositiveInt stepCount)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Length < 5 || descriptionStr.Length < 10)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create valid value objects
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var categoryId = CategoryId.NewId();

        // Create steps (at least 1, max 10)
        var actualStepCount = Math.Min(Math.Max(1, stepCount.Get), 10);
        var steps = Enumerable.Range(1, actualStepCount)
            .Select(i => TipStep.Create(i, $"Step number {i} description"))
            .ToList();

        // Act: Create a new tip
        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);

        // Assert: Initial soft delete state should be false/null
        tip.IsDeleted.Should().BeFalse("newly created tips should not be marked as deleted");
        tip.DeletedAt.Should().BeNull("newly created tips should not have a deletion timestamp");
    }

    // Feature: firestore-test-infrastructure-improvements, Property 3: MarkDeleted Idempotence and Behavior
    // For any User, Tip, or Category entity, calling MarkDeleted should set IsDeleted to true and
    // DeletedAt to a recent UTC timestamp, and calling MarkDeleted multiple times should produce
    // the same result as calling it once (idempotent operation).
    // Validates: Requirements 2.3, 2.4, 3.3, 3.4, 10.2

    /// <summary>
    /// Property: Calling MarkDeleted on a Tip should set IsDeleted to true and DeletedAt to a recent UTC timestamp.
    /// This property verifies the basic behavior of the MarkDeleted method.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldSetIsDeletedTrueAndDeletedAtToUtcNow_WhenCalled(
        NonEmptyString title,
        NonEmptyString description,
        PositiveInt stepCount)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Length < 5 || descriptionStr.Length < 10)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a new tip
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var categoryId = CategoryId.NewId();

        var actualStepCount = Math.Min(Math.Max(1, stepCount.Get), 10);
        var steps = Enumerable.Range(1, actualStepCount)
            .Select(i => TipStep.Create(i, $"Step number {i} description"))
            .ToList();

        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);
        var beforeDelete = DateTime.UtcNow;

        // Act: Mark the tip as deleted
        tip.MarkDeleted();
        var afterDelete = DateTime.UtcNow;

        // Assert: IsDeleted should be true
        tip.IsDeleted.Should().BeTrue("MarkDeleted should set IsDeleted to true");

        // Assert: DeletedAt should be set to a recent UTC timestamp
        tip.DeletedAt.Should().NotBeNull("MarkDeleted should set DeletedAt");
        tip.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete, "DeletedAt should be after the method was called");
        tip.DeletedAt.Value.Should().BeOnOrBefore(afterDelete, "DeletedAt should be before the method completed");
        tip.DeletedAt.Value.Kind.Should().Be(DateTimeKind.Utc, "DeletedAt should be in UTC");
    }

    /// <summary>
    /// Property: Calling MarkDeleted multiple times should be idempotent (same result as calling once).
    /// This property verifies that MarkDeleted can be safely called multiple times.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldBeIdempotent_WhenCalledMultipleTimes(
        NonEmptyString title,
        NonEmptyString description,
        PositiveInt stepCount,
        PositiveInt callCount)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Length < 5 || descriptionStr.Length < 10)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a new tip
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var categoryId = CategoryId.NewId();

        var actualStepCount = Math.Min(Math.Max(1, stepCount.Get), 10);
        var steps = Enumerable.Range(1, actualStepCount)
            .Select(i => TipStep.Create(i, $"Step number {i} description"))
            .ToList();

        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);

        // Act: Call MarkDeleted once and capture the state
        tip.MarkDeleted();
        var firstIsDeleted = tip.IsDeleted;
        var firstDeletedAt = tip.DeletedAt;

        // Act: Call MarkDeleted multiple additional times (1-10 times)
        var additionalCalls = Math.Min(Math.Max(1, callCount.Get), 10);
        for (int i = 0; i < additionalCalls; i++)
        {
            // Small delay to ensure time would change if the method wasn't idempotent
            Thread.Sleep(1);
            tip.MarkDeleted();
        }

        // Assert: State should be unchanged after additional calls
        tip.IsDeleted.Should().Be(firstIsDeleted, "IsDeleted should not change on subsequent calls");
        tip.DeletedAt.Should().Be(firstDeletedAt, "DeletedAt should not change on subsequent calls (idempotent)");
    }

    /// <summary>
    /// Property: MarkDeleted should have no effect when called on an already deleted tip.
    /// This property verifies the early return behavior for already-deleted entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MarkDeleted_ShouldReturnEarly_WhenTipIsAlreadyDeleted(
        NonEmptyString title,
        NonEmptyString description,
        PositiveInt stepCount)
    {
        // Precondition: Ensure title and description meet minimum length requirements
        var titleStr = title.Get;
        var descriptionStr = description.Get;

        if (titleStr.Length < 5 || descriptionStr.Length < 10)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create and delete a tip
        var tipTitle = TipTitle.Create(titleStr);
        var tipDescription = TipDescription.Create(descriptionStr);
        var categoryId = CategoryId.NewId();

        var actualStepCount = Math.Min(Math.Max(1, stepCount.Get), 10);
        var steps = Enumerable.Range(1, actualStepCount)
            .Select(i => TipStep.Create(i, $"Step number {i} description"))
            .ToList();

        var tip = Tip.Create(tipTitle, tipDescription, steps, categoryId);
        tip.MarkDeleted();

        var originalDeletedAt = tip.DeletedAt;

        // Act: Wait a bit and call MarkDeleted again
        Thread.Sleep(10);
        tip.MarkDeleted();

        // Assert: DeletedAt should not have changed
        tip.DeletedAt.Should().Be(originalDeletedAt, "DeletedAt should not change when already deleted");
        tip.IsDeleted.Should().BeTrue("IsDeleted should remain true");
    }
}
