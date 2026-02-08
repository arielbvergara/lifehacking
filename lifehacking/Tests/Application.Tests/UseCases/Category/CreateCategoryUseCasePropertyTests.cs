using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

/// <summary>
/// Property-based tests for CreateCategoryUseCase.
/// Feature: admin-category-management
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public class CreateCategoryUseCasePropertyTests
{
    // Feature: admin-category-management, Property 2: Case-insensitive uniqueness on creation
    // For any existing category name, attempting to create a new category with the same name 
    // (regardless of casing) should return HTTP 409 Conflict.
    // Validates: Requirements 1.5

    /// <summary>
    /// Property: Creating a category with a name that differs only in casing from an existing
    /// category should fail with ConflictException.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 2: Case-insensitive uniqueness on creation")]
    public async Task CaseInsensitiveUniqueness_ShouldReturnConflict_WhenCreatingWithDifferentCasing(
        NonEmptyString nameGen)
    {
        // Precondition: Ensure name meets minimum length requirements (2-100 characters after trimming)
        var categoryName = nameGen.Get.Trim();

        if (categoryName.Length < 2 || categoryName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a use case with mocked repository
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);

        // Create the original category with the generated name
        var originalCategory = DomainCategory.Create(categoryName);

        // Set up the mock to return null for the first call (original creation succeeds)
        // and return the existing category for subsequent calls (different casing fails)
        var callCount = 0;
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken ct) =>
            {
                callCount++;
                // First call: no existing category (original creation)
                if (callCount == 1)
                {
                    return null;
                }
                // Subsequent calls: return existing category if names match case-insensitively
                if (string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return originalCategory;
                }
                return null;
            });

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act 1: Create the original category
        var originalRequest = new CreateCategoryRequest(categoryName);
        var originalResult = await useCase.ExecuteAsync(originalRequest);

        // Assert: Original creation should succeed
        originalResult.IsSuccess.Should().BeTrue(
            $"original category creation with name '{categoryName}' should succeed");

        // Act 2: Attempt to create with uppercase version
        var uppercaseRequest = new CreateCategoryRequest(categoryName.ToUpperInvariant());
        var uppercaseResult = await useCase.ExecuteAsync(uppercaseRequest);

        // Assert: Uppercase version should fail with ConflictException
        uppercaseResult.IsFailure.Should().BeTrue(
            $"creating category with uppercase name '{categoryName.ToUpperInvariant()}' should fail");
        uppercaseResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException for duplicate name");

        // Act 3: Attempt to create with lowercase version
        var lowercaseRequest = new CreateCategoryRequest(categoryName.ToLowerInvariant());
        var lowercaseResult = await useCase.ExecuteAsync(lowercaseRequest);

        // Assert: Lowercase version should fail with ConflictException
        lowercaseResult.IsFailure.Should().BeTrue(
            $"creating category with lowercase name '{categoryName.ToLowerInvariant()}' should fail");
        lowercaseResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException for duplicate name");

        // Act 4: Attempt to create with mixed case version (if different from original)
        var mixedCaseName = GenerateMixedCase(categoryName);
        if (!string.Equals(mixedCaseName, categoryName, StringComparison.Ordinal))
        {
            var mixedCaseRequest = new CreateCategoryRequest(mixedCaseName);
            var mixedCaseResult = await useCase.ExecuteAsync(mixedCaseRequest);

            // Assert: Mixed case version should fail with ConflictException
            mixedCaseResult.IsFailure.Should().BeTrue(
                $"creating category with mixed case name '{mixedCaseName}' should fail");
            mixedCaseResult.Error.Should().BeOfType<ConflictException>(
                "error should be ConflictException for duplicate name");
        }
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

    // Feature: admin-category-management, Property 3: Soft-deleted categories block name reuse on creation
    // For any soft-deleted category, attempting to create a new category with the same name 
    // should return HTTP 409 Conflict.
    // Validates: Requirements 1.6

    /// <summary>
    /// Property: Creating a category with the same name as a soft-deleted category should fail
    /// with ConflictException.
    /// **Validates: Requirements 1.6**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 3: Soft-deleted categories block name reuse on creation")]
    public async Task SoftDeletedCategoryBlocksNameReuse_ShouldReturnConflict_WhenCreatingWithSameName(
        NonEmptyString nameGen)
    {
        // Precondition: Ensure name meets minimum length requirements (2-100 characters after trimming)
        var categoryName = nameGen.Get.Trim();

        if (categoryName.Length < 2 || categoryName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a use case with mocked repository
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);

        // Create a category and mark it as soft-deleted
        var softDeletedCategory = DomainCategory.Create(categoryName);
        softDeletedCategory.MarkDeleted();

        // Set up the mock to return null for the first call (original creation succeeds)
        // and return the soft-deleted category for subsequent calls (reuse attempt fails)
        var callCount = 0;
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken ct) =>
            {
                callCount++;
                // First call: no existing category (original creation)
                if (callCount == 1)
                {
                    return null;
                }
                // Subsequent calls: return soft-deleted category if names match case-insensitively
                if (includeDeleted && string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return softDeletedCategory;
                }
                return null;
            });

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act 1: Create the original category
        var originalRequest = new CreateCategoryRequest(categoryName);
        var originalResult = await useCase.ExecuteAsync(originalRequest);

        // Assert: Original creation should succeed
        originalResult.IsSuccess.Should().BeTrue(
            $"original category creation with name '{categoryName}' should succeed");

        // Simulate soft-deleting the category (in real scenario, this would be done via DeleteCategoryUseCase)
        // The mock is already set up to return the soft-deleted category for subsequent calls

        // Act 2: Attempt to create a new category with the same name as the soft-deleted one
        var reuseRequest = new CreateCategoryRequest(categoryName);
        var reuseResult = await useCase.ExecuteAsync(reuseRequest);

        // Assert: Reuse attempt should fail with ConflictException
        reuseResult.IsFailure.Should().BeTrue(
            $"creating category with same name '{categoryName}' as soft-deleted category should fail");
        reuseResult.Error.Should().BeOfType<ConflictException>(
            "error should be ConflictException when attempting to reuse soft-deleted category name");
        reuseResult.Error.Should().NotBeNull("error should be present when result is failure");
        reuseResult.Error!.Message.Should().Contain("already exists",
            "error message should indicate the name already exists");

        // Verify that the repository was called with includeDeleted: true
        categoryRepositoryMock.Verify(
            x => x.GetByNameAsync(categoryName, true, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "repository should check for soft-deleted categories when validating uniqueness");
    }
}
