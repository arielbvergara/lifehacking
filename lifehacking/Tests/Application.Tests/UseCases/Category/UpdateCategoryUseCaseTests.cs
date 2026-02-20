using Application.Dtos.Category;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

public class UpdateCategoryUseCaseTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationServiceMock;
    private readonly UpdateCategoryUseCase _useCase;

    public UpdateCategoryUseCaseTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _cacheInvalidationServiceMock = new Mock<ICacheInvalidationService>();
        _useCase = new UpdateCategoryUseCase(
            _categoryRepositoryMock.Object,
            _cacheInvalidationServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenNameIsValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("New Valid Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Valid Name");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("New Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain("Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("New Name");

        // Repository returns null for soft-deleted categories
        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain("Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationExceptionWithFieldLevelDetail_WhenNameIsTooShort()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("A"); // 1 character, less than minimum of 2

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().ContainSingle()
            .Which.Should().Contain("at least 2 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationExceptionWithFieldLevelDetail_WhenNameIsTooLong()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var longName = new string('A', 101); // 101 characters, exceeds maximum of 100
        var request = new UpdateCategoryRequest(longName);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().ContainSingle()
            .Which.Should().Contain("cannot exceed 100 characters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenNewNameExistsOnDifferentCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var otherCategory = DomainCategory.Create("Existing Name");
        var request = new UpdateCategoryRequest("Existing Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenNewNameMatchesSoftDeletedCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var softDeletedCategory = DomainCategory.Create("Deleted Name");
        softDeletedCategory.MarkDeleted();
        var request = new UpdateCategoryRequest("Deleted Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(softDeletedCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByNameAsyncWithIncludeDeleted_WhenCheckingUniqueness()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("New Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateAsync_WhenNameIsValidAndUnique()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("New Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainCategory>(c => c.Name == "New Name" && !c.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrimCategoryName_WhenNameHasWhitespace()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("  Trimmed Name  ");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationExceptionWithFieldLevelDetail_WhenNameIsEmpty()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().ContainSingle()
            .Which.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationExceptionWithFieldLevelDetail_WhenNameIsWhitespace()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("   ");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationException>().Subject;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().ContainSingle()
            .Which.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenNameIsExactlyMinLength()
    {
        // Arrange - Test boundary: exactly 2 characters should be valid
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("AB");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("AB");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenNameIsExactlyMaxLength()
    {
        // Arrange - Test boundary: exactly 100 characters should be valid
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var maxLengthName = new string('A', 100);
        var request = new UpdateCategoryRequest(maxLengthName);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenUpdatingToSameName()
    {
        // Arrange - Updating a category to its own name should succeed
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);

        // Create a category with the specific ID using FromPersistence
        var existingCategory = DomainCategory.FromPersistence(
            categoryIdValueObject,
            "Same Name",
            DateTime.UtcNow.AddDays(-1),
            null,
            false,
            null);

        var request = new UpdateCategoryRequest("Same Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.Is<CategoryId>(id => id.Value == categoryId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // When checking by name, return the same category (simulating that the name exists but it's the same category)
        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        // This should succeed because the use case checks if existingCategory.Id != categoryId
        // Since we're using the same category, the IDs match and it should allow the update
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Same Name");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenNameExistsCaseInsensitive()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var otherCategory = DomainCategory.Create("existing name");
        var request = new UpdateCategoryRequest("EXISTING NAME");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCategory);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetUpdatedAt_WhenNameIsUpdated()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = DomainCategory.Create("Old Name");
        var request = new UpdateCategoryRequest("New Name");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId, request);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainCategory>(c => c.UpdatedAt.HasValue && c.UpdatedAt.Value <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
