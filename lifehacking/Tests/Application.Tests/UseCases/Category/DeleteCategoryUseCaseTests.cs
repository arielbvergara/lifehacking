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

public class DeleteCategoryUseCaseTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly DeleteCategoryUseCase _useCase;

    public DeleteCategoryUseCaseTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _useCase = new DeleteCategoryUseCase(
            _categoryRepositoryMock.Object,
            _tipRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenCategoryExistsWithoutTips()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenCategoryExistsWithTips()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);
        var category = DomainCategory.Create("Test Category");

        var tip1 = DomainTip.Create(
            TipTitle.Create("Tip 1"),
            TipDescription.Create("Description 1"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tip2 = DomainTip.Create(
            TipTitle.Create("Tip 2"),
            TipDescription.Create("Description 2"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tips = new List<DomainTip> { tip1, tip2 };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenCategoryIsAlreadySoftDeleted()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Repository returns null for soft-deleted categories
        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        // Act
        var result = await _useCase.ExecuteAsync(categoryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Category");
        notFoundError.Message.Should().Contain(categoryId.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetIsDeletedAndDeletedAt_WhenCategoryIsDeleted()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainCategory>(c =>
                    c.IsDeleted == true &&
                    c.DeletedAt.HasValue &&
                    c.DeletedAt.Value <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetIsDeletedAndDeletedAtForAllTips_WhenCategoryWithTipsIsDeleted()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);
        var category = DomainCategory.Create("Test Category");

        var tip1 = DomainTip.Create(
            TipTitle.Create("Tip 1"),
            TipDescription.Create("Description 1"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tip2 = DomainTip.Create(
            TipTitle.Create("Tip 2"),
            TipDescription.Create("Description 2"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tips = new List<DomainTip> { tip1, tip2 };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainTip>(t =>
                    t.IsDeleted == true &&
                    t.DeletedAt.HasValue &&
                    t.DeletedAt.Value <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByCategoryAsync_WhenDeletingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.GetByCategoryAsync(
                It.Is<CategoryId>(id => id.Value == categoryId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateAsyncForCategory_WhenDeletingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainCategory>(c => c.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateAsyncForEachTip_WhenDeletingCategoryWithTips()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);
        var category = DomainCategory.Create("Test Category");

        var tip1 = DomainTip.Create(
            TipTitle.Create("Tip 1"),
            TipDescription.Create("Description 1"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tip2 = DomainTip.Create(
            TipTitle.Create("Tip 2"),
            TipDescription.Create("Description 2"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tip3 = DomainTip.Create(
            TipTitle.Create("Tip 3"),
            TipDescription.Create("Description 3"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tips = new List<DomainTip> { tip1, tip2, tip3 };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainTip>(t => t.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCallTipUpdateAsync_WhenCategoryHasNoTips()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create("Test Category");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkCategoryDeletedBeforeTips_WhenDeletingCategoryWithTips()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);
        var category = DomainCategory.Create("Test Category");

        var tip = DomainTip.Create(
            TipTitle.Create("Tip 1"),
            TipDescription.Create("Description 1"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryIdValueObject);

        var tips = new List<DomainTip> { tip };

        var callOrder = new List<string>();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("category"))
            .Returns(Task.CompletedTask);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("tip"))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(categoryId);

        // Assert
        callOrder.Should().HaveCount(2);
        callOrder[0].Should().Be("category");
        callOrder[1].Should().Be("tip");
    }
}
