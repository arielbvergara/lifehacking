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

public class CreateTipUseCaseTests
{
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly CreateTipUseCase _useCase;

    public CreateTipUseCaseTests()
    {
        _tipRepositoryMock = new Mock<ITipRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _useCase = new CreateTipUseCase(_tipRepositoryMock.Object, _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters"),
                new(2, "Second step with enough characters")
            },
            CategoryId: categoryId,
            Tags: new List<string> { "tag1", "tag2" },
            VideoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Valid Tip Title");
        result.Value.Description.Should().Be("This is a valid tip description with enough characters");
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Tags.Should().HaveCount(2);
        result.Value.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        result.Value.CategoryName.Should().Be("Test Category");
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenTitleIsEmpty()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("title cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenTitleIsTooShort()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Tip", // 3 characters, less than minimum of 5
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("at least 5 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenDescriptionIsEmpty()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("description cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenDescriptionIsTooShort()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "Short", // 5 characters, less than minimum of 10
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenStepsAreEmpty()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>(), // Empty steps list
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenStepsAreNull()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: null!, // Null steps
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Category does not exist");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Deleted Category");
        category.MarkDeleted();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Cannot assign tip to a deleted category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenVideoUrlIsInvalid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://invalid-url.com/video"
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("supported platform");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenVideoUrlIsValidYouTube()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenVideoUrlIsValidInstagram()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://www.instagram.com/p/ABC123"
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VideoUrl.Should().Be("https://www.instagram.com/p/ABC123");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenVideoUrlIsValidYouTubeShorts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://www.youtube.com/shorts/ABC123"
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VideoUrl.Should().Be("https://www.youtube.com/shorts/ABC123");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenVideoUrlIsNull()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.VideoUrl.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenTagsAreNull()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallAddAsync_WhenInputIsValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<DomainTip>(t =>
                    t.Title.Value == "Valid Tip Title" &&
                    !t.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByIdAsync_WhenCheckingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip t, CancellationToken _) => t);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.Is<CategoryId>(c => c.Value == categoryId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
