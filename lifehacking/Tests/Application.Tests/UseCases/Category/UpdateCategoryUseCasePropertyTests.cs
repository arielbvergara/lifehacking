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
            .ReturnsAsync((string name, bool _, CancellationToken _) =>
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
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken _) =>
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

    // Feature: admin-api-validation-strengthening, Property: Valid names always succeed
    // For any valid category name (2-100 characters), update should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a category with a valid name (2-100 characters) should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Valid names always succeed")]
    public async Task ValidNames_ShouldAlwaysSucceed_WhenLengthBetween2And100(NonEmptyString nameGen)
    {
        // Generate a valid name (2-100 characters)
        var baseName = nameGen.Get.Trim();

        // Ensure the name is within valid range
        if (baseName.Length < DomainCategory.MinNameLength)
        {
            baseName = baseName.PadRight(DomainCategory.MinNameLength, 'a');
        }
        if (baseName.Length > DomainCategory.MaxNameLength)
        {
            baseName = baseName.Substring(0, DomainCategory.MaxNameLength);
        }

        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.FromPersistence(
            CategoryId.Create(categoryId),
            "Original Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null); // No name conflict

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new UpdateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"updating category with valid name '{baseName}' (length {baseName.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
        result.Value!.Name.Should().Be(baseName.Trim(), "category name should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Names < 2 chars always fail
    // For any category name shorter than 2 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a category with a name shorter than 2 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Names < 2 chars always fail")]
    public async Task TooShortNames_ShouldAlwaysFail_WhenLengthLessThan2(NonEmptyString nameGen)
    {
        // Generate a name that's too short (0-1 characters after trimming)
        var baseName = nameGen.Get.Trim();

        // Skip if already valid length or empty (empty is tested separately)
        if (baseName.Length >= DomainCategory.MinNameLength || baseName.Length == 0)
        {
            baseName = "a"; // Exactly 1 character
        }
        else if (baseName.Length > 1)
        {
            baseName = baseName.Substring(0, 1); // Take only 1 character
        }

        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.FromPersistence(
            CategoryId.Create(categoryId),
            "Original Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new UpdateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating category with too-short name '{baseName}' (length {baseName.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-short name");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Name",
            "validation error should include Name field");
        var errorMessages = string.Join(" ", validationError.Errors["Name"]);
        errorMessages.Should().Contain("at least",
            "error message should mention minimum length requirement");
    }

    // Feature: admin-api-validation-strengthening, Property: Names > 100 chars always fail
    // For any category name longer than 100 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a category with a name longer than 100 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Names > 100 chars always fail")]
    public async Task TooLongNames_ShouldAlwaysFail_WhenLengthGreaterThan100(NonEmptyString nameGen)
    {
        // Generate a name that's too long (> 100 characters after trimming)
        var baseName = nameGen.Get.Trim();

        // Ensure the name is actually longer than max length
        if (baseName.Length <= DomainCategory.MaxNameLength)
        {
            baseName = baseName.PadRight(DomainCategory.MaxNameLength + 1, 'x');
        }

        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.FromPersistence(
            CategoryId.Create(categoryId),
            "Original Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new UpdateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating category with too-long name (length {baseName.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long name");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Name",
            "validation error should include Name field");
        var errorMessages = string.Join(" ", validationError.Errors["Name"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }

    // Feature: admin-api-validation-strengthening, Property: Boundary values always succeed
    // For category names with exactly 2 or exactly 100 characters, update should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2), US-5 (Acceptance Criteria 3)

    /// <summary>
    /// Property: Updating a category with exactly 2 characters should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2), US-5 (Acceptance Criteria 3)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Boundary values always succeed")]
    public async Task BoundaryMinimum_ShouldAlwaysSucceed_WhenExactly2Characters(char c1, char c2)
    {
        // Generate a name with exactly 2 characters (minimum boundary)
        var categoryName = $"{c1}{c2}".Trim();

        // Skip if trimming resulted in less than 2 characters
        if (categoryName.Length < 2)
        {
            return;
        }

        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.FromPersistence(
            CategoryId.Create(categoryId),
            "Original Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new UpdateCategoryRequest(categoryName);

        // Act
        var result = await useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"updating category with exactly {DomainCategory.MinNameLength} characters should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
    }

    /// <summary>
    /// Property: Updating a category with exactly 100 characters should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2), US-5 (Acceptance Criteria 3)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Boundary values always succeed")]
    public async Task BoundaryMaximum_ShouldAlwaysSucceed_WhenExactly100Characters(NonEmptyString nameGen)
    {
        // Generate a name with exactly 100 characters (maximum boundary)
        var baseName = nameGen.Get.Trim();
        var categoryName = baseName.Length >= DomainCategory.MaxNameLength
            ? baseName.Substring(0, DomainCategory.MaxNameLength)
            : baseName.PadRight(DomainCategory.MaxNameLength, 'a');

        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.FromPersistence(
            CategoryId.Create(categoryId),
            "Original Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new UpdateCategoryRequest(categoryName);

        // Act
        var result = await useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"updating category with exactly {DomainCategory.MaxNameLength} characters should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
        result.Value!.Name.Should().HaveLength(DomainCategory.MaxNameLength,
            "category name should have exactly 100 characters");
    }
}
