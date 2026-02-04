using Application.Dtos;
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

public class SearchTipsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedTipsResponse_WhenTipsExist()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var categoryId = CategoryId.NewId();
        const string categoryName = "Test Category";
        var category = DomainCategory.FromPersistence(categoryId, categoryName, DateTime.UtcNow, null);

        var tip1 = CreateTestTip("Tip 1", "Description 1", categoryId);
        var tip2 = CreateTestTip("Tip 2", "Description 2", categoryId);
        var tips = new List<DomainTip> { tip1, tip2 };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 2));

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Title.Should().Be("Tip 1");
        result.Value.Items[0].CategoryName.Should().Be(categoryName);
        result.Value.Items[1].Title.Should().Be("Tip 2");
        result.Value.Items[1].CategoryName.Should().Be(categoryName);
        result.Value.Metadata.TotalItems.Should().Be(2);
        result.Value.Metadata.PageNumber.Should().Be(1);
        result.Value.Metadata.PageSize.Should().Be(20);
        result.Value.Metadata.TotalPages.Should().Be(1);

        tipRepositoryMock.Verify(
            r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyResults_WhenNoTipsFound()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var criteria = new TipQueryCriteria(
            SearchTerm: "nonexistent",
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DomainTip>(), 0));

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Metadata.TotalItems.Should().Be(0);
        result.Value.Metadata.PageNumber.Should().Be(1);
        result.Value.Metadata.PageSize.Should().Be(20);
        result.Value.Metadata.TotalPages.Should().Be(0);

        tipRepositoryMock.Verify(
            r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMultipleCategories_WhenTipsHaveDifferentCategories()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var categoryId1 = CategoryId.NewId();
        var categoryId2 = CategoryId.NewId();
        const string categoryName1 = "Category 1";
        const string categoryName2 = "Category 2";

        var category1 = DomainCategory.FromPersistence(categoryId1, categoryName1, DateTime.UtcNow, null);
        var category2 = DomainCategory.FromPersistence(categoryId2, categoryName2, DateTime.UtcNow, null);

        var tip1 = CreateTestTip("Tip 1", "Description 1", categoryId1);
        var tip2 = CreateTestTip("Tip 2", "Description 2", categoryId2);
        var tips = new List<DomainTip> { tip1, tip2 };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 2));

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category1);

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category2);

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].CategoryName.Should().Be(categoryName1);
        result.Value.Items[1].CategoryName.Should().Be(categoryName2);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId1, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMissingCategory_WhenCategoryNotFound()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var categoryId = CategoryId.NewId();
        var tip = CreateTestTip("Tip 1", "Description 1", categoryId);
        var tips = new List<DomainTip> { tip };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 1));

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].CategoryName.Should().Be("Unknown Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculatePaginationCorrectly_WhenMultiplePagesExist()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var categoryId = CategoryId.NewId();
        const string categoryName = "Test Category";
        var category = DomainCategory.FromPersistence(categoryId, categoryName, DateTime.UtcNow, null);

        var tip = CreateTestTip("Tip 1", "Description 1", categoryId);
        var tips = new List<DomainTip> { tip };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 2,
            PageSize: 5
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 23)); // Total of 23 items

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Metadata.TotalItems.Should().Be(23);
        result.Value.Metadata.PageNumber.Should().Be(2);
        result.Value.Metadata.PageSize.Should().Be(5);
        result.Value.Metadata.TotalPages.Should().Be(5); // Ceiling(23/5) = 5
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfraException_WhenRepositoryThrowsException()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20
        );

        tipRepositoryMock
            .Setup(r => r.SearchAsync(criteria, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var request = new SearchTipsRequest(criteria);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.Message.Should().Be("An error occurred while searching for tips.");
        result.Error.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new SearchTipsUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));

        tipRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()),
            Times.Never);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DomainTip CreateTestTip(string title, string description, CategoryId categoryId)
    {
        var steps = new[] { TipStep.Create(1, "This is a test step with enough characters") };
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
            null);
    }
}