using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Tip;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Tip;

/// <summary>
/// Property-based tests for CreateTipUseCase.
/// Feature: admin-api-validation-strengthening
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public class CreateTipUseCasePropertyTests
{
    // Feature: admin-api-validation-strengthening, Property: Valid titles always succeed
    // For any valid tip title (5-200 characters), creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a valid title (5-200 characters) should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Valid titles always succeed")]
    public async Task ValidTitles_ShouldAlwaysSucceed_WhenLengthBetween5And200(NonEmptyString titleGen)
    {
        // Generate a valid title (5-200 characters)
        var baseTitle = titleGen.Get.Trim();

        // Ensure the title is within valid range
        if (baseTitle.Length < TipTitle.MinLength)
        {
            baseTitle = baseTitle.PadRight(TipTitle.MinLength, 'a');
        }
        if (baseTitle.Length > TipTitle.MaxLength)
        {
            baseTitle = baseTitle.Substring(0, TipTitle.MaxLength);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        // Create a valid category
        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        // Create a valid request with the generated title
        var request = new CreateTipRequest(
            Title: baseTitle,
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: categoryId.Value,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating tip with valid title '{baseTitle}' (length {baseTitle.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Title.Should().Be(baseTitle.Trim(), "tip title should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Titles < 5 chars always fail
    // For any tip title shorter than 5 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a title shorter than 5 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Titles < 5 chars always fail")]
    public async Task TooShortTitles_ShouldAlwaysFail_WhenLengthLessThan5(NonEmptyString titleGen)
    {
        // Generate a title that's too short (1-4 characters after trimming)
        var baseTitle = titleGen.Get.Trim();

        // Ensure the title is actually too short
        if (baseTitle.Length >= TipTitle.MinLength || baseTitle.Length == 0)
        {
            baseTitle = "abcd"; // Exactly 4 characters
        }
        else if (baseTitle.Length >= TipTitle.MinLength)
        {
            baseTitle = baseTitle.Substring(0, TipTitle.MinLength - 1);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: baseTitle,
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-short title '{baseTitle}' (length {baseTitle.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-short title");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Title",
            "validation error should include Title field");
        var errorMessages = string.Join(" ", validationError.Errors["Title"]);
        errorMessages.Should().Contain("at least",
            "error message should mention minimum length requirement");
    }

    // Feature: admin-api-validation-strengthening, Property: Titles > 200 chars always fail
    // For any tip title longer than 200 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a title longer than 200 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Titles > 200 chars always fail")]
    public async Task TooLongTitles_ShouldAlwaysFail_WhenLengthGreaterThan200(NonEmptyString titleGen)
    {
        // Generate a title that's too long (> 200 characters after trimming)
        var baseTitle = titleGen.Get.Trim();

        // Ensure the title is actually longer than max length
        if (baseTitle.Length <= TipTitle.MaxLength)
        {
            baseTitle = baseTitle.PadRight(TipTitle.MaxLength + 1, 'x');
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: baseTitle,
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-long title (length {baseTitle.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long title");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Title",
            "validation error should include Title field");
        var errorMessages = string.Join(" ", validationError.Errors["Title"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }

    // Feature: admin-api-validation-strengthening, Property: Valid descriptions always succeed
    // For any valid tip description (10-2000 characters), creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a valid description (10-2000 characters) should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Valid descriptions always succeed")]
    public async Task ValidDescriptions_ShouldAlwaysSucceed_WhenLengthBetween10And2000(NonEmptyString descGen)
    {
        // Generate a valid description (10-2000 characters)
        var baseDescription = descGen.Get.Trim();

        // Ensure the description is within valid range
        if (baseDescription.Length < TipDescription.MinLength)
        {
            baseDescription = baseDescription.PadRight(TipDescription.MinLength, 'a');
        }
        if (baseDescription.Length > TipDescription.MaxLength)
        {
            baseDescription = baseDescription.Substring(0, TipDescription.MaxLength);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: baseDescription,
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: categoryId.Value,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating tip with valid description (length {baseDescription.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Description.Should().Be(baseDescription.Trim(), "tip description should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Descriptions < 10 chars always fail
    // For any tip description shorter than 10 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a description shorter than 10 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Descriptions < 10 chars always fail")]
    public async Task TooShortDescriptions_ShouldAlwaysFail_WhenLengthLessThan10(NonEmptyString descGen)
    {
        // Generate a description that's too short (1-9 characters after trimming)
        var baseDescription = descGen.Get.Trim();

        // Ensure the description is actually too short
        if (baseDescription.Length >= TipDescription.MinLength || baseDescription.Length == 0)
        {
            baseDescription = "short"; // 5 characters
        }
        else if (baseDescription.Length >= TipDescription.MinLength)
        {
            baseDescription = baseDescription.Substring(0, TipDescription.MinLength - 1);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: baseDescription,
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-short description '{baseDescription}' (length {baseDescription.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-short description");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Description",
            "validation error should include Description field");
        var errorMessages = string.Join(" ", validationError.Errors["Description"]);
        errorMessages.Should().Contain("at least",
            "error message should mention minimum length requirement");
    }

    // Feature: admin-api-validation-strengthening, Property: Descriptions > 2000 chars always fail
    // For any tip description longer than 2000 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with a description longer than 2000 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Descriptions > 2000 chars always fail")]
    public async Task TooLongDescriptions_ShouldAlwaysFail_WhenLengthGreaterThan2000(NonEmptyString descGen)
    {
        // Generate a description that's too long (> 2000 characters after trimming)
        var baseDescription = descGen.Get.Trim();

        // Ensure the description is actually longer than max length
        if (baseDescription.Length <= TipDescription.MaxLength)
        {
            baseDescription = baseDescription.PadRight(TipDescription.MaxLength + 1, 'x');
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: baseDescription,
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-long description (length {baseDescription.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long description");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Description",
            "validation error should include Description field");
        var errorMessages = string.Join(" ", validationError.Errors["Description"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }

    // Feature: admin-api-validation-strengthening, Property: Valid step descriptions always succeed
    // For any valid step description (10-500 characters), creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with valid step descriptions (10-500 characters) should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Valid step descriptions always succeed")]
    public async Task ValidStepDescriptions_ShouldAlwaysSucceed_WhenLengthBetween10And500(NonEmptyString stepDescGen)
    {
        // Generate a valid step description (10-500 characters)
        var baseStepDesc = stepDescGen.Get.Trim();

        // Ensure the step description is within valid range
        if (baseStepDesc.Length < 10) // TipStep.MinDescriptionLength
        {
            baseStepDesc = baseStepDesc.PadRight(10, 'a');
        }
        if (baseStepDesc.Length > 500) // TipStep.MaxDescriptionLength
        {
            baseStepDesc = baseStepDesc.Substring(0, 500);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, baseStepDesc)
            },
            CategoryId: categoryId.Value,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating tip with valid step description (length {baseStepDesc.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Steps.Should().HaveCount(1, "tip should have one step");
        result.Value!.Steps[0].Description.Should().Be(baseStepDesc.Trim(), "step description should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Step descriptions < 10 chars always fail
    // For any step description shorter than 10 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with step descriptions shorter than 10 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Step descriptions < 10 chars always fail")]
    public async Task TooShortStepDescriptions_ShouldAlwaysFail_WhenLengthLessThan10(NonEmptyString stepDescGen)
    {
        // Generate a step description that's too short (1-9 characters after trimming)
        var baseStepDesc = stepDescGen.Get.Trim();

        // Ensure the step description is actually too short
        if (baseStepDesc.Length >= 10 || baseStepDesc.Length == 0)
        {
            baseStepDesc = "short"; // 5 characters
        }
        else if (baseStepDesc.Length >= 10)
        {
            baseStepDesc = baseStepDesc.Substring(0, 9);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, baseStepDesc)
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-short step description '{baseStepDesc}' (length {baseStepDesc.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-short step description");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Steps[0]",
            "validation error should include Steps[0] field");
        var errorMessages = string.Join(" ", validationError.Errors["Steps[0]"]);
        errorMessages.Should().Contain("at least",
            "error message should mention minimum length requirement");
    }

    // Feature: admin-api-validation-strengthening, Property: Step descriptions > 500 chars always fail
    // For any step description longer than 500 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with step descriptions longer than 500 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Step descriptions > 500 chars always fail")]
    public async Task TooLongStepDescriptions_ShouldAlwaysFail_WhenLengthGreaterThan500(NonEmptyString stepDescGen)
    {
        // Generate a step description that's too long (> 500 characters after trimming)
        var baseStepDesc = stepDescGen.Get.Trim();

        // Ensure the step description is actually longer than max length
        if (baseStepDesc.Length <= 500)
        {
            baseStepDesc = baseStepDesc.PadRight(501, 'x');
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, baseStepDesc)
            },
            CategoryId: Guid.NewGuid(),
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-long step description (length {baseStepDesc.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long step description");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Steps[0]",
            "validation error should include Steps[0] field");
        var errorMessages = string.Join(" ", validationError.Errors["Steps[0]"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }

    // Feature: admin-api-validation-strengthening, Property: Valid tags always succeed
    // For any valid tag (1-50 characters), creation should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with valid tags (1-50 characters) should always succeed.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Valid tags always succeed")]
    public async Task ValidTags_ShouldAlwaysSucceed_WhenLengthBetween1And50(NonEmptyString tagGen)
    {
        // Generate a valid tag (1-50 characters)
        var baseTag = tagGen.Get.Trim();

        // Ensure the tag is within valid range
        if (baseTag.Length < 1)
        {
            baseTag = "a";
        }
        if (baseTag.Length > 50)
        {
            baseTag = baseTag.Substring(0, 50);
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: categoryId.Value,
            Tags: new List<string> { baseTag },
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"creating tip with valid tag '{baseTag}' (length {baseTag.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Tags.Should().HaveCount(1, "tip should have one tag");
        result.Value!.Tags[0].Should().Be(baseTag.Trim(), "tag should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Tags > 50 chars always fail
    // For any tag longer than 50 characters, creation should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Creating a tip with tags longer than 50 characters should always fail
    /// with ValidationException.
    /// **Validates: Requirements US-2 (Acceptance Criteria 1, 2)**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-api-validation-strengthening")]
    [Trait("Property", "Tags > 50 chars always fail")]
    public async Task TooLongTags_ShouldAlwaysFail_WhenLengthGreaterThan50(NonEmptyString tagGen)
    {
        // Generate a tag that's too long (> 50 characters after trimming)
        var baseTag = tagGen.Get.Trim();

        // Ensure the tag is actually longer than max length
        if (baseTag.Length <= 50)
        {
            baseTag = baseTag.PadRight(51, 'x');
        }

        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new CreateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new CreateTipRequest(
            Title: "Valid Title Here",
            Description: "This is a valid description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new TipStepRequest(1, "This is a valid step description")
            },
            CategoryId: Guid.NewGuid(),
            Tags: new List<string> { baseTag },
            VideoUrl: null
        );

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"creating tip with too-long tag (length {baseTag.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long tag");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Tags[0]",
            "validation error should include Tags[0] field");
        var errorMessages = string.Join(" ", validationError.Errors["Tags[0]"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }
}
