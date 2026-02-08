using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

/// <summary>
/// Property-based tests for UpdateCategoryUseCase.
/// Feature: admin-category-management
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public class UpdateCategoryUseCasePropertyTests
{
    // Feature: admin-category-management, Property 5: Case-insensitive uniqueness on update
    // For any two existing categories, attempting to update one category to have the same name 
    // as the other (regardless of casing) should return HTTP 409 Conflict.
    // Validates: Requirements 2.7

    /// <summary>
    /// Property: Updating a category to have the same name as another existing category
    /// (regardless of casing) should fail with ConflictException.
    /// **Validates: Requirements 2.7**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 5: Case-insensitive uniqueness on update")]
    public async Task CaseInsensitiveUniquenessOnUpdate_ShouldReturnConflict_WhenUpdatingToDifferentCasing(
        NonEmptyString firstNameGen,
        NonEmptyString secondNameGen)
    {
        // Precondition: Ensure names meet minimum length requirements (2-100 characters after trimming)
        var firstName = firstNameGen.Get.Trim();
        var secondName = secondNameGen.Get.Trim();

        if (firstName.Length < 2 || firstName.Length > 100 ||
            secondName.Length < 2 || secondName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Precondition: Ensure the two names are different (case-insensitive)
        // We want to test updating from one name to another, not updating to the same name
        if (string.Equals(firstName, secondName, StringComparison.OrdinalIgnoreCase))
        {
            return; // Skip if names are the same
        }

        // Arrange: Create a use case with mocked repository
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);

        // Create two categories with different names
        var firstCategoryId = Guid.NewGuid();
        var secondCategoryId = Guid.NewGuid();

        var firstCategory = DomainCategory.FromPersistence(
            CategoryId.Create(firstCategoryId),
            firstName,
            DateTime.UtcNow.AddDays(-2),
            null,
            false,
            null);

        var secondCategory = DomainCategory.FromPersistence(
            CategoryId.Create(secondCategoryId),
            secondName,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        // Set up the mock to return the first category when queried by its ID
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == firstCategoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstCategory);

        // Set up the mock to return the second category when checking for name uniqueness
        // with any casing variation of the second category's name
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken ct) =>
            {
                // Return the second category if the name matches (case-insensitive)
                if (string.Equals(name, secondName, StringComparison.OrdinalIgnoreCase))
                {
                    return secondCategory;
                }
                return null;
            });

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act 1: Attempt to update first category to have second category's name (uppercase)
        var uppercaseRequest = new UpdateCategoryRequest(secondName.ToUpperInvariant());
        var uppercaseResult = await useCase.ExecuteAsync(firstCategoryId, uppercaseRequest);

        // Assert: Uppercase version should fail with ConflictException
        uppercaseResult.IsFailure.Should().BeTrue(
            $"updating category to uppercase name '{secondName.ToUpperInvariant()}' should fail when another category has that name");
        uppercaseResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException for duplicate name");
        uppercaseResult.Error!.Message.Should().Contain("already exists",
            "error message should indicate the name already exists");

        // Act 2: Attempt to update first category to have second category's name (lowercase)
        var lowercaseRequest = new UpdateCategoryRequest(secondName.ToLowerInvariant());
        var lowercaseResult = await useCase.ExecuteAsync(firstCategoryId, lowercaseRequest);

        // Assert: Lowercase version should fail with ConflictException
        lowercaseResult.IsFailure.Should().BeTrue(
            $"updating category to lowercase name '{secondName.ToLowerInvariant()}' should fail when another category has that name");
        lowercaseResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException for duplicate name");
        lowercaseResult.Error!.Message.Should().Contain("already exists",
            "error message should indicate the name already exists");

        // Act 3: Attempt to update first category to have second category's name (mixed case)
        var mixedCaseName = GenerateMixedCase(secondName);
        if (!string.Equals(mixedCaseName, secondName, StringComparison.Ordinal))
        {
            var mixedCaseRequest = new UpdateCategoryRequest(mixedCaseName);
            var mixedCaseResult = await useCase.ExecuteAsync(firstCategoryId, mixedCaseRequest);

            // Assert: Mixed case version should fail with ConflictException
            mixedCaseResult.IsFailure.Should().BeTrue(
                $"updating category to mixed case name '{mixedCaseName}' should fail when another category has that name");
            mixedCaseResult.Error.Should().BeOfType<ConflictException>(
                "error should be ConflictException for duplicate name");
            mixedCaseResult.Error!.Message.Should().Contain("already exists",
                "error message should indicate the name already exists");
        }

        // Verify that the repository was called with includeDeleted: true
        categoryRepositoryMock.Verify(
            x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "repository should check for soft-deleted categories when validating uniqueness");
    }

    /// <summary>
    /// Generates a mixed case version of a string by alternating upper and lower case.
    /// </summary>
    private static string GenerateMixedCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    // Feature: admin-category-management, Property 6: Soft-deleted categories block name reuse on update
    // For any existing category and any soft-deleted category, attempting to update the existing 
    // category to have the same name as the soft-deleted category should return HTTP 409 Conflict.
    // Validates: Requirements 2.8

    /// <summary>
    /// Property: Updating a category to have the same name as a soft-deleted category should fail
    /// with ConflictException.
    /// **Validates: Requirements 2.8**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 6: Soft-deleted categories block name reuse on update")]
    public async Task SoftDeletedCategoryBlocksNameReuseOnUpdate_ShouldReturnConflict_WhenUpdatingToSoftDeletedName(
        NonEmptyString activeNameGen,
        NonEmptyString deletedNameGen)
    {
        // Precondition: Ensure names meet minimum length requirements (2-100 characters after trimming)
        var activeName = activeNameGen.Get.Trim();
        var deletedName = deletedNameGen.Get.Trim();

        if (activeName.Length < 2 || activeName.Length > 100 ||
            deletedName.Length < 2 || deletedName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Precondition: Ensure the two names are different (case-insensitive)
        // We want to test updating from one name to another, not updating to the same name
        if (string.Equals(activeName, deletedName, StringComparison.OrdinalIgnoreCase))
        {
            return; // Skip if names are the same
        }

        // Arrange: Create a use case with mocked repository
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);

        // Create an active category
        var activeCategoryId = Guid.NewGuid();
        var activeCategory = DomainCategory.FromPersistence(
            CategoryId.Create(activeCategoryId),
            activeName,
            DateTime.UtcNow.AddDays(-2),
            null,
            false,
            null);

        // Create a soft-deleted category
        var deletedCategoryId = Guid.NewGuid();
        var deletedCategory = DomainCategory.FromPersistence(
            CategoryId.Create(deletedCategoryId),
            deletedName,
            DateTime.UtcNow.AddDays(-3),
            null,
            true,
            DateTime.UtcNow.AddDays(-1));

        // Set up the mock to return the active category when queried by its ID
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == activeCategoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCategory);

        // Set up the mock to return the soft-deleted category when checking for name uniqueness
        // with any casing variation of the deleted category's name
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken ct) =>
            {
                // Return the soft-deleted category if the name matches (case-insensitive)
                // and includeDeleted is true
                if (includeDeleted && string.Equals(name, deletedName, StringComparison.OrdinalIgnoreCase))
                {
                    return deletedCategory;
                }
                return null;
            });

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: Attempt to update active category to have soft-deleted category's name
        var updateRequest = new UpdateCategoryRequest(deletedName);
        var updateResult = await useCase.ExecuteAsync(activeCategoryId, updateRequest);

        // Assert: Update should fail with ConflictException
        updateResult.IsFailure.Should().BeTrue(
            $"updating category to name '{deletedName}' should fail when a soft-deleted category has that name");
        updateResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException when attempting to reuse soft-deleted category name");
        updateResult.Error!.Message.Should().Contain("already exists",
            "error message should indicate the name already exists");

        // Verify that the repository was called with includeDeleted: true
        categoryRepositoryMock.Verify(
            x => x.GetByNameAsync(deletedName, true, It.IsAny<CancellationToken>()),
            Times.Once(),
            "repository should check for soft-deleted categories when validating uniqueness");

        // Verify that UpdateAsync was NOT called (update should have failed before reaching this point)
        categoryRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()),
            Times.Never(),
            "repository UpdateAsync should not be called when uniqueness validation fails");
    }
}
