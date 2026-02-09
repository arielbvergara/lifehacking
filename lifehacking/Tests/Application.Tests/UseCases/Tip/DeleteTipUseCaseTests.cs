using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Tip;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Tip;

public class DeleteTipUseCaseTests
{
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly DeleteTipUseCase _useCase;

    public DeleteTipUseCaseTests()
    {
        _tipRepositoryMock = new Mock<ITipRepository>();
        _useCase = new DeleteTipUseCase(_tipRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenTipExists()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Test Tip Title"),
            TipDescription.Create("Test tip description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Test step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(tipId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var tipId = Guid.NewGuid();

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        // Act
        var result = await _useCase.ExecuteAsync(tipId);

        // Assert
        result.IsFailure.Should().BeTrue();
        var notFoundError = result.Error.Should().BeOfType<NotFoundException>().Subject;
        notFoundError.Message.Should().Contain("Tip");
        notFoundError.Message.Should().Contain(tipId.ToString());
        notFoundError.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetIsDeletedAndDeletedAt_WhenTipIsDeleted()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Test Tip Title"),
            TipDescription.Create("Test tip description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Test step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(tipId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainTip>(t =>
                    t.IsDeleted == true &&
                    t.DeletedAt.HasValue &&
                    t.DeletedAt.Value <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallGetByIdAsync_WhenDeletingTip()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Test Tip Title"),
            TipDescription.Create("Test tip description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Test step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(tipId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.Is<TipId>(id => id.Value == tipId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateAsync_WhenTipExists()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var existingTip = DomainTip.Create(
            TipTitle.Create("Test Tip Title"),
            TipDescription.Create("Test tip description with enough characters"),
            new List<TipStep> { TipStep.Create(1, "Test step with enough characters") },
            CategoryId.Create(categoryId),
            new List<Tag>()
        );

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);

        _tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(tipId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DomainTip>(t => t.IsDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCallUpdateAsync_WhenTipDoesNotExist()
    {
        // Arrange
        var tipId = Guid.NewGuid();

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        // Act
        await _useCase.ExecuteAsync(tipId);

        // Assert
        _tipRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
