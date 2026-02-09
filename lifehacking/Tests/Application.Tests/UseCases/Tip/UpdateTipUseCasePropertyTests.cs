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
/// Property-based tests for UpdateTipUseCase.
/// Feature: admin-api-validation-strengthening
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public class UpdateTipUseCasePropertyTests
{
    // Feature: admin-api-validation-strengthening, Property: Valid titles always succeed
    // For any valid tip title (5-200 characters), update should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a valid title (5-200 characters) should always succeed.
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        // Create a valid category
        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        // Create a valid request with the generated title
        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"updating tip with valid title '{baseTitle}' (length {baseTitle.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Title.Should().Be(baseTitle.Trim(), "tip title should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Titles < 5 chars always fail
    // For any tip title shorter than 5 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a title shorter than 5 characters should always fail
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating tip with too-short title '{baseTitle}' (length {baseTitle.Length}) should fail");
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
    // For any tip title longer than 200 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a title longer than 200 characters should always fail
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating tip with too-long title (length {baseTitle.Length}) should fail");
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
    // For any valid tip description (10-2000 characters), update should succeed.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a valid description (10-2000 characters) should always succeed.
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        var category = DomainCategory.Create("Test Category");
        var categoryId = CategoryId.Create(Guid.NewGuid());

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue(
            $"updating tip with valid description (length {baseDescription.Length}) should succeed");
        result.Value.Should().NotBeNull("successful result should contain tip response");
        result.Value!.Description.Should().Be(baseDescription.Trim(), "tip description should match the trimmed input");
    }

    // Feature: admin-api-validation-strengthening, Property: Descriptions < 10 chars always fail
    // For any tip description shorter than 10 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a description shorter than 10 characters should always fail
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating tip with too-short description '{baseDescription}' (length {baseDescription.Length}) should fail");
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
    // For any tip description longer than 2000 characters, update should fail with ValidationException.
    // Validates: Requirements US-2 (Acceptance Criteria 1, 2)

    /// <summary>
    /// Property: Updating a tip with a description longer than 2000 characters should always fail
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
        var tipId = Guid.NewGuid();
        var existingTip = DomainTip.FromPersistence(
            TipId.Create(tipId),
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step description") },
            CategoryId.Create(Guid.NewGuid()),
            new List<Tag>(),
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();

        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        var useCase = new UpdateTipUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var request = new UpdateTipRequest(
            Id: tipId,
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
        var result = await useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue(
            $"updating tip with too-long description (length {baseDescription.Length}) should fail");
        result.Error.Should().BeOfType<ValidationException>(
            "error should be ValidationException for too-long description");

        var validationError = (ValidationException)result.Error!;
        validationError.Errors.Should().ContainKey("Description",
            "validation error should include Description field");
        var errorMessages = string.Join(" ", validationError.Errors["Description"]);
        errorMessages.Should().Contain("cannot exceed",
            "error message should mention maximum length constraint");
    }
}
