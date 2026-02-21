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
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationServiceMock;
    private readonly CreateTipUseCase _useCase;

    public CreateTipUseCaseTests()
    {
        _tipRepositoryMock = new Mock<ITipRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _cacheInvalidationServiceMock = new Mock<ICacheInvalidationService>();
        _useCase = new CreateTipUseCase(
            _tipRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _cacheInvalidationServiceMock.Object);
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Title));
        validationError.Errors[nameof(request.Title)].Should().Contain(e => e.Contains("title cannot be empty"));
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Title));
        validationError.Errors[nameof(request.Title)].Should().Contain(e => e.Contains("at least 5 characters"));
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Description));
        validationError.Errors[nameof(request.Description)].Should().Contain(e => e.Contains("description cannot be empty"));
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Description));
        validationError.Errors[nameof(request.Description)].Should().Contain(e => e.Contains("at least 10 characters"));
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Steps));
        validationError.Errors[nameof(request.Steps)].Should().Contain(e => e.Contains("At least one step is required"));
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Steps));
        validationError.Errors[nameof(request.Steps)].Should().Contain(e => e.Contains("At least one step is required"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryDoesNotExist()
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
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryIsSoftDeleted()
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
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
        notFoundError.Message.Should().Contain("not found");
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
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.VideoUrl));
        validationError.Errors[nameof(request.VideoUrl)].Should().Contain(e => e.Contains("supported platform"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Tip", // Too short
            Description: "Short", // Too short
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: "https://invalid-url.com/video" // Invalid URL
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Title));
        validationError.Errors.Should().ContainKey(nameof(request.Description));
        validationError.Errors.Should().ContainKey(nameof(request.VideoUrl));
        validationError.Errors[nameof(request.Title)].Should().Contain(e => e.Contains("at least 5 characters"));
        validationError.Errors[nameof(request.Description)].Should().Contain(e => e.Contains("at least 10 characters"));
        validationError.Errors[nameof(request.VideoUrl)].Should().Contain(e => e.Contains("supported platform"));
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

    [Fact]
    public async Task ExecuteAsync_ShouldCreateTipWithImage_WhenValidImageProvided()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        var imageDto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/tips/test-image.jpg",
            ImageStoragePath: "tips/550e8400-e29b-41d4-a716-446655440000.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 245760,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
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
        result.Value!.Image.Should().NotBeNull();
        result.Value.Image!.ImageUrl.Should().Be(imageDto.ImageUrl);
        result.Value.Image.ImageStoragePath.Should().Be(imageDto.ImageStoragePath);
        result.Value.Image.OriginalFileName.Should().Be(imageDto.OriginalFileName);
        result.Value.Image.ContentType.Should().Be(imageDto.ContentType);
        result.Value.Image.FileSizeBytes.Should().Be(imageDto.FileSizeBytes);
        result.Value.Image.UploadedAt.Should().BeCloseTo(imageDto.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateTipWithoutImage_WhenImageNotProvided()
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
            VideoUrl: null,
            Image: null
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
        result.Value!.Image.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenImageUrlInvalid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var imageDto = new TipImageDto(
            ImageUrl: "not-a-valid-url",
            ImageStoragePath: "tips/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 1000,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Image.ImageUrl");
        validationError.Errors["Image.ImageUrl"].Should().Contain(e => e.Contains("valid absolute URL"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenImageContentTypeInvalid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var imageDto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/tips/test.bmp",
            ImageStoragePath: "tips/test.bmp",
            OriginalFileName: "test.bmp",
            ContentType: "image/bmp",
            FileSizeBytes: 1000,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Image.ContentType");
        validationError.Errors["Image.ContentType"].Should().Contain(e =>
            e.Contains("image/jpeg") || e.Contains("image/png") || e.Contains("image/gif") || e.Contains("image/webp"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenImageFileSizeExceedsLimit()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var imageDto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/tips/test.jpg",
            ImageStoragePath: "tips/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 10485760, // 10MB, exceeds 5MB limit
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough characters",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Image.FileSizeBytes");
        validationError.Errors["Image.FileSizeBytes"].Should().Contain(e => e.Contains("5242880") || e.Contains("5MB"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAggregateErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        var imageDto = new TipImageDto(
            ImageUrl: "not-a-valid-url",
            ImageStoragePath: "tips/test.jpg",
            OriginalFileName: "test.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 1000,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateTipRequest(
            Title: "Tip", // Too short
            Description: "Short", // Too short
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough characters")
            },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null,
            Image: imageDto
        );

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey(nameof(request.Title));
        validationError.Errors.Should().ContainKey(nameof(request.Description));
        validationError.Errors.Should().ContainKey("Image.ImageUrl");
    }
}
