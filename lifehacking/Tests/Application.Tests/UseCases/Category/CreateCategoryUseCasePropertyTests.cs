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
            .ReturnsAsync((string name, bool _, CancellationToken _) =>
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

    // Feature: admin-api-validation-strengthening, Property: Valid names always succeed
    // For any valid category name (2-100 characters), creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a category with a valid name (2-100 characters) should always succeed.
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
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null); // No existing category

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating category with valid name '{baseName}' (length {baseName.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
        result.Value!.Name.Should().Be(baseName.Trim(), "category name should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Names < 2 chars always fail
    // For any category name shorter than 2 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a category with a name shorter than 2 characters should always fail
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
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating category with too-short name '{baseName}' (length {baseName.Length}) should fail");
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
    // For any category name longer than 100 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a category with a name longer than 100 characters should always fail
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
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(baseName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating category with too-long name (length {baseName.Length}) should fail");
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
    // For category names with exactly 2 or exactly 100 characters, creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2), US-5 (Acceptance Criteria 3)

    /// <summary>
    /// Property: Creating a category with exactly 2 characters should always succeed.
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
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(categoryName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating category with exactly {DomainCategory.MinNameLength} characters should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
    }

    /// <summary>
    /// Property: Creating a category with exactly 100 characters should always succeed.
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
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(categoryName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating category with exactly {DomainCategory.MaxNameLength} characters should succeed");
        result.Value.Should().NotBeNull("successful result should contain category response");
        result.Value!.Name.Should().HaveLength(DomainCategory.MaxNameLength,
            "category name should have exactly 100 characters");
    }

    // Feature: admin-api-validation-strengthening, Property: Whitespace-only names always fail
    // For any category name containing only whitespace, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a category with a whitespace-only name should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Whitespace-only names always fail")]
    public async Task WhitespaceOnlyNames_ShouldAlwaysFail_WhenNameIsOnlyWhitespace(PositiveInt lengthGen)
    {
        // Generate a whitespace-only name (spaces, tabs, newlines)
        var length = lengthGen.Get % 20 + 1; // 1-20 whitespace characters
        var whitespaceChars = new[] { ' ', '\t', '\n', '\r' };
        var random = new Random(lengthGen.Get);
        var categoryName = new string(Enumerable.Range(0, length)
            .Select(_ => whitespaceChars[random.Next(whitespaceChars.Length)])
            .ToArray());

        // Arrange
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(categoryName);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            "creating category with whitespace-only name should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for whitespace-only name");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Name",
            "validation error should include Name field");
        var errorMessages = string.Join(" ", validationError.Errors["Name"]);
        errorMessages.Should().Contain("cannot be empty",
            "error message should indicate name cannot be empty");
    }

    // Feature: admin-api-validation-strengthening, Property: Names with leading/trailing whitespace are trimmed
    // For any category name with leading or trailing whitespace, the whitespace should be trimmed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a category with leading/trailing whitespace should trim the whitespace
    /// and succeed if the trimmed name is valid.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Names with leading/trailing whitespace are trimmed")]
    public async Task LeadingTrailingWhitespace_ShouldBeTrimmed_WhenCreatingCategory(
        NonEmptyString nameGen,
        PositiveInt leadingSpaces,
        PositiveInt trailingSpaces)
    {
        // Generate a valid base name
        var baseName = nameGen.Get.Trim();
        if (baseName.Length < DomainCategory.MinNameLength)
        {
            baseName = baseName.PadRight(DomainCategory.MinNameLength, 'a');
        }
        if (baseName.Length > DomainCategory.MaxNameLength)
        {
            baseName = baseName.Substring(0, DomainCategory.MaxNameLength);
        }

        // Add leading and trailing whitespace
        var leading = new string(' ', leadingSpaces.Get % 10 + 1); // 1-10 leading spaces
        var trailing = new string(' ', trailingSpaces.Get % 10 + 1); // 1-10 trailing spaces
        var categoryNameWithWhitespace = $"{leading}{baseName}{trailing}";

        // Arrange
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        var useCase = new CreateCategoryUseCase(categoryRepositoryMock.Object);
        var request = new CreateCategoryRequest(categoryNameWithWhitespace);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating category with leading/trailing whitespace should succeed after trimming");
        result.Value.Should().NotBeNull("successful result should contain category response");
        result.Value!.Name.Should().Be(baseName,
            "category name should be trimmed (no leading/trailing whitespace)");
        result.Value!.Name.Should().NotStartWith(" ", "trimmed name should not start with space");
        result.Value!.Name.Should().NotEndWith(" ", "trimmed name should not end with space");
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
            .ReturnsAsync((string name, bool includeDeleted, CancellationToken _) =>
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
