using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

public class CreateCategoryUseCaseTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly CreateCategoryUseCase _useCase;

    public CreateCategoryUseCaseTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _useCase = new CreateCategoryUseCase(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenNameIsValid()
    {
        // Arrange
        var request = new CreateCategoryRequest("Valid Category Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Valid Category Name");
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenNameIsTooShort()
    {
        // Arrange
        var request = new CreateCategoryRequest("A"); // 1 character, less than minimum of 2

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("at least 2 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenNameIsTooLong()
    {
        // Arrange
        var longName = new string('A', 101); // 101 characters, exceeds maximum of 100
        var request = new CreateCategoryRequest(longName);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("cannot exceed 100 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenNameAlreadyExists()
    {
        // Arrange
        var request = new CreateCategoryRequest("Existing Category");
        var existingCategory = DomainCategory.Create("Existing Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenNameExistsCaseInsensitive()
    {
        // Arrange
        var request = new CreateCategoryRequest("EXISTING CATEGORY");
        var existingCategory = DomainCategory.Create("existing category");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenSoftDeletedCategoryHasSameName()
    {
        // Arrange
        var request = new CreateCategoryRequest("Deleted Category");
        var softDeletedCategory = DomainCategory.Create("Deleted Category");
        softDeletedCategory.MarkDeleted();

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(softDeletedCategory);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByNameAsyncWithIncludeDeleted_WhenCheckingUniqueness()
    {
        // Arrange
        var request = new CreateCategoryRequest("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallAddAsync_WhenNameIsValidAndUnique()
    {
        // Arrange
        var request = new CreateCategoryRequest("New Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<DomainCategory>(c => c.Name == "New Category" && !c.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrimCategoryName_WhenNameHasWhitespace()
    {
        // Arrange
        var request = new CreateCategoryRequest("  Trimmed Category  ");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Trimmed Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateCategoryRequest("");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenNameIsWhitespace()
    {
        // Arrange
        var request = new CreateCategoryRequest("   ");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenNameIsExactlyMinLength()
    {
        // Arrange - Test boundary: exactly 2 characters should be valid
        var request = new CreateCategoryRequest("AB");

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("AB");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenNameIsExactlyMaxLength()
    {
        // Arrange - Test boundary: exactly 100 characters should be valid
        var maxLengthName = new string('A', 100);
        var request = new CreateCategoryRequest(maxLengthName);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory c, CancellationToken _) => c);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(maxLengthName);
    }
}
