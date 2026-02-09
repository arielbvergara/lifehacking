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
    private readonly UpdateTipUseCase _useCase;

    public UpdateTipUseCaseTests()
    {
        _tipRepositoryMock = new Mock<ITipRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _useCase = new UpdateTipUseCase(_tipRepositoryMock.Object, _categoryRepositoryMock.Object);
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
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain("Tip");
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("title cannot be empty");
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("description cannot be empty");
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenCategoryDoesNotExist()
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Category does not exist");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenCategoryIsSoftDeleted()
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Cannot assign tip to a deleted category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSpecificError_WhenUpdatingToDeletedCategory()
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Be("Cannot assign tip to a deleted category");
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
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("supported platform");
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
}
