using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

public class GetCategoriesUseCaseTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly IMemoryCache _memoryCache;
    private readonly GetCategoriesUseCase _useCase;

    public GetCategoriesUseCaseTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _useCase = new GetCategoriesUseCase(
            _categoryRepositoryMock.Object,
            _tipRepositoryMock.Object,
            _memoryCache);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainCategory>());

        // Act
        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();

        _categoryRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _tipRepositoryMock.Verify(
            r => r.CountByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllCategories_WhenCategoriesExist()
    {
        // Arrange
        var category1 = DomainCategory.FromPersistence(
            CategoryId.NewId(),
            "Technology",
            DateTime.UtcNow.AddDays(-10),
            null,
            false,
            null);

        var category2 = DomainCategory.FromPersistence(
            CategoryId.NewId(),
            "Health",
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            false,
            null);

        var category3 = DomainCategory.FromPersistence(
            CategoryId.NewId(),
            "Finance",
            DateTime.UtcNow.AddDays(-3),
            null,
            false,
            null);

        var categories = new List<DomainCategory> { category1, category2, category3 };

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        _tipRepositoryMock
            .Setup(r => r.CountByCategoryAsync(category1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _tipRepositoryMock
            .Setup(r => r.CountByCategoryAsync(category2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _tipRepositoryMock
            .Setup(r => r.CountByCategoryAsync(category3.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items[0].Id.Should().Be(category1.Id.Value);
        result.Value.Items[0].Name.Should().Be("Technology");
        result.Value.Items[0].TipCount.Should().Be(5);
        result.Value.Items[0].UpdatedAt.Should().BeNull();
        result.Value.Items[1].Id.Should().Be(category2.Id.Value);
        result.Value.Items[1].Name.Should().Be("Health");
        result.Value.Items[1].TipCount.Should().Be(3);
        result.Value.Items[1].UpdatedAt.Should().NotBeNull();
        result.Value.Items[2].Id.Should().Be(category3.Id.Value);
        result.Value.Items[2].Name.Should().Be("Finance");
        result.Value.Items[2].TipCount.Should().Be(0);

        _categoryRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _tipRepositoryMock.Verify(
            r => r.CountByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeDeletedCategories_WhenSomeAreDeleted()
    {
        // Arrange
        var activeCategory = DomainCategory.FromPersistence(
            CategoryId.NewId(),
            "Active Category",
            DateTime.UtcNow,
            null,
            false,
            null);

        DomainCategory.FromPersistence(
            CategoryId.NewId(),
            "Deleted Category",
            DateTime.UtcNow.AddDays(-10),
            null,
            true,
            DateTime.UtcNow.AddDays(-1));

        // Repository should only return non-deleted categories
        var categories = new List<DomainCategory> { activeCategory };

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Id.Should().Be(activeCategory.Id.Value);
        result.Value.Items[0].Name.Should().Be("Active Category");

        _categoryRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfrastructureException_WhenRepositoryThrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failed");

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.Message.Should().Be("Failed to retrieve categories");
        result.Error.InnerException.Should().Be(expectedException);

        _categoryRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
