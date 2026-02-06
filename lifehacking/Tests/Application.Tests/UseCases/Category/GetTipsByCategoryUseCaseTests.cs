using Application.Dtos;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Category;

public class GetTipsByCategoryUseCaseTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly GetTipsByCategoryUseCase _useCase;

    public GetTipsByCategoryUseCaseTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _useCase = new GetTipsByCategoryUseCase(
            _categoryRepositoryMock.Object,
            _tipRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenCategoryIdIsInvalid()
    {
        // Arrange
        const string invalidCategoryId = "not-a-guid";
        var request = new GetTipsByCategoryRequest();

        // Act
        var result = await _useCase.ExecuteAsync(invalidCategoryId, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Invalid category ID format");
        result.Error.Message.Should().Contain(invalidCategoryId);

        _categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        var request = new GetTipsByCategoryRequest();

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain($"Category with ID '{categoryId.Value}' not found");

        _categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenCategoryHasNoTips()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        const string categoryName = "Empty Category";
        var request = new GetTipsByCategoryRequest();

        var category = DomainCategory.FromPersistence(
            categoryId,
            categoryName,
            DateTime.UtcNow,
            null,
            false,
            null);

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DomainTip>(), 0));

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Metadata.TotalItems.Should().Be(0);
        result.Value.Metadata.PageNumber.Should().Be(1);
        result.Value.Metadata.PageSize.Should().Be(10);
        result.Value.Metadata.TotalPages.Should().Be(0);

        _categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPaginatedTips_WhenCategoryHasTips()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        const string categoryName = "Technology";
        var request = new GetTipsByCategoryRequest
        {
            PageNumber = 1,
            PageSize = 2
        };

        var category = DomainCategory.FromPersistence(
            categoryId,
            categoryName,
            DateTime.UtcNow,
            null,
            false,
            null);

        var tip1 = CreateTestTip(categoryId, "Tip 1", "Description 1");
        var tip2 = CreateTestTip(categoryId, "Tip 2", "Description 2");
        var tips = new List<DomainTip> { tip1, tip2 };

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 5)); // Total of 5 tips, returning first 2

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Title.Should().Be("Tip 1");
        result.Value.Items[0].CategoryName.Should().Be(categoryName);
        result.Value.Items[1].Title.Should().Be("Tip 2");
        result.Value.Metadata.TotalItems.Should().Be(5);
        result.Value.Metadata.PageNumber.Should().Be(1);
        result.Value.Metadata.PageSize.Should().Be(2);
        result.Value.Metadata.TotalPages.Should().Be(3);

        _categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.Is<TipQueryCriteria>(c =>
                c.CategoryId == categoryId.Value &&
                c.PageNumber == 1 &&
                c.PageSize == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenPageNumberIsInvalid()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        var request = new GetTipsByCategoryRequest
        {
            PageNumber = 0
        };

        var category = DomainCategory.FromPersistence(
            categoryId,
            "Test Category",
            DateTime.UtcNow,
            null,
            false,
            null);

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Page number must be greater than or equal to 1");

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationException_WhenPageSizeIsInvalid()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        var request = new GetTipsByCategoryRequest
        {
            PageSize = 101 // Exceeds maximum
        };

        var category = DomainCategory.FromPersistence(
            categoryId,
            "Test Category",
            DateTime.UtcNow,
            null,
            false,
            null);

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("Page size must be between 1 and 100");

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApplyDefaultPagination_WhenParametersAreOmitted()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        var request = new GetTipsByCategoryRequest(); // No pagination parameters

        var category = DomainCategory.FromPersistence(
            categoryId,
            "Test Category",
            DateTime.UtcNow,
            null,
            false,
            null);

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DomainTip>(), 0));

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Metadata.PageNumber.Should().Be(1); // Default
        result.Value.Metadata.PageSize.Should().Be(10); // Default

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.Is<TipQueryCriteria>(c =>
                c.PageNumber == 1 &&
                c.PageSize == 10), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApplyDefaultSorting_WhenParametersAreOmitted()
    {
        // Arrange
        var categoryId = CategoryId.NewId();
        var request = new GetTipsByCategoryRequest(); // No sorting parameters

        var category = DomainCategory.FromPersistence(
            categoryId,
            "Test Category",
            DateTime.UtcNow,
            null,
            false,
            null);

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DomainTip>(), 0));

        // Act
        var result = await _useCase.ExecuteAsync(categoryId.Value.ToString(), request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _tipRepositoryMock.Verify(
            r => r.SearchAsync(It.Is<TipQueryCriteria>(c =>
                c.SortField == TipSortField.CreatedAt &&
                c.SortDirection == SortDirection.Descending), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static DomainTip CreateTestTip(CategoryId categoryId, string title, string description)
    {
        var steps = new[] { TipStep.Create(1, "This is a test step with enough characters to be valid") };
        var tags = new[] { Tag.Create("test") };

        return DomainTip.FromPersistence(
            TipId.NewId(),
            TipTitle.Create(title),
            TipDescription.Create(description),
            steps,
            categoryId,
            tags,
            null,
            DateTime.UtcNow,
            null,
            false,
            null);
    }
}
