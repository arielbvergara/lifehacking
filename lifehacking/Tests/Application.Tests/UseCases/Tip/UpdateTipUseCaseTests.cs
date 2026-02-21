using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Tip;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Tip;

public class UpdateTipUseCaseTests
{
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationServiceMock;
    private readonly UpdateTipUseCase _useCase;

    public UpdateTipUseCaseTests()
    {
        _tipRepositoryMock = new Mock<ITipRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _cacheInvalidationServiceMock = new Mock<ICacheInvalidationService>();
        _useCase = new UpdateTipUseCase(
            _tipRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _cacheInvalidationServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters"),
                new(2, "Second updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: new List<string> { "tag1", "tag2" },
            VideoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Updated Tip Title");
        result.Value.Description.Should().Be("This is an updated tip description with enough characters");
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Tags.Should().HaveCount(2);
        result.Value.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        result.Value.CategoryName.Should().Be("Test Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Tip");
        notFoundError.Message.Should().Contain(tipId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenTitleIsEmpty()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Title));
        validationError.Errors[nameof(request.Title)].Should().Contain(e => e.Contains("title cannot be empty"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenDescriptionIsEmpty()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Description));
        validationError.Errors[nameof(request.Description)].Should().Contain(e => e.Contains("description cannot be empty"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenStepsAreEmpty()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>(), // Empty steps list
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Steps));
        validationError.Errors[nameof(request.Steps)].Should().Contain(e => e.Contains("At least one step is required"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Deleted Category");
        category.MarkDeleted();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenUpdatingToDeletedCategory()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Deleted Category");
        category.MarkDeleted();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenVideoUrlIsInvalid()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://invalid-url.com/video"
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.VideoUrl));
        validationError.Errors[nameof(request.VideoUrl)].Should().Contain(e => e.Contains("supported platform"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateAsync_WhenInputIsValid()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(tipId, request);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainTip>(t =>
                    t.Title.Value == "Updated Tip Title" &&
                    !t.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByIdAsync_WhenCheckingCategory()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(tipId, request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.Is<CategoryId>(c => c.Value == categoryId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInvalidateCache_WhenUpdateSucceeds()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var oldCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var category = DomainCategory.Create("New Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(oldCategoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: newCategoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategoryAndList(
                It.Is<CategoryId>(c => c.Value == newCategoryId)),
            Times.Once,
            "New category cache and list should be invalidated when tip update succeeds");
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategory(
                It.Is<CategoryId>(c => c.Value == oldCategoryId)),
            Times.Once,
            "Old category cache should be invalidated when tip category changes");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotInvalidateCache_WhenValidationFails()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "", // Invalid: empty title
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()),
            Times.Never,
            "Cache should not be invalidated when validation fails");
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategory(It.IsAny<CategoryId>()),
            Times.Never,
            "Category cache should not be invalidated when validation fails");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotInvalidateCache_WhenTipNotFound()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()),
            Times.Never,
            "Cache should not be invalidated when tip is not found");
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategory(It.IsAny<CategoryId>()),
            Times.Never,
            "Category cache should not be invalidated when tip is not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotInvalidateCache_WhenCategoryNotFound()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()),
            Times.Never,
            "Cache should not be invalidated when category is not found");
        _cacheInvalidationServiceMock.Verify(
            x => x.InvalidateCategory(It.IsAny<CategoryId>()),
            Times.Never,
            "Category cache should not be invalidated when category is not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidImageProvided()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var imageDto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/images/test.jpg",
            ImageStoragePath: "tips/test-guid/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 102400,
            UploadedAt: DateTime.UtcNow
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Image.Should().NotBeNull();
        result.Value.Image!.ImageUrl.Should().Be("https://cdn.example.com/images/test.jpg");
        result.Value.Image.ImageStoragePath.Should().Be("tips/test-guid/test.jpg");
        result.Value.Image.OriginalFileName.Should().Be("test.jpg");
        result.Value.Image.ContentType.Should().Be("image/jpeg");
        result.Value.Image.FileSizeBytes.Should().Be(102400);
        result.Value.Image.UploadedAt.Should().Be(imageDto.UploadedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenImageIsNull()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: null
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Image.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenImageUrlIsInvalid()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var imageDto = new TipImageDto(
            ImageUrl: "", // Invalid: empty URL
            ImageStoragePath: "tips/test-guid/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 102400,
            UploadedAt: DateTime.UtcNow
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Image.ImageUrl");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenImageContentTypeIsInvalid()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Original Title"),
            TipDescription.Create("Original description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Original step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        var imageDto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/images/test.jpg",
            ImageStoragePath: "tips/test-guid/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "", // Invalid: empty content type
            FileSizeBytes: 102400,
            UploadedAt: DateTime.UtcNow
        );

        var request = new UpdateTipRequest(
            Id: tipId,
            Title: "Updated Tip Title",
            Description: "This is an updated tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First updated step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        // Act
        var result = await _useCase.ExecuteAsync(tipId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Image.ContentType");
    }
}
