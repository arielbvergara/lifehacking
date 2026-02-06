using Application.Dtos.Favorite;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Favorite;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainUser = Domain.Entities.User;

namespace Application.Tests.UseCases.Favorite;

public class RemoveFavoriteUseCaseTests
{
    private readonly Mock<IFavoritesRepository> _favoritesRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly RemoveFavoriteUseCase _useCase;

    public RemoveFavoriteUseCaseTests()
    {
        _favoritesRepositoryMock = new Mock<IFavoritesRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _useCase = new RemoveFavoriteUseCase(
            _favoritesRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTrue_WhenFavoriteRemovedSuccessfully()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var existingFavorite = UserFavorites.Create(userId, tipId);

        var request = new RemoveFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFavorite);

        _favoritesRepositoryMock
            .Setup(x => x.RemoveAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var request = new RemoveFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUser?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain(userId.Value.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenFavoriteDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var request = new RemoveFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain("not found in user's favorites");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _useCase.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallRepositoryRemoveAsync_WhenFavoriteExists()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var existingFavorite = UserFavorites.Create(userId, tipId);

        var request = new RemoveFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFavorite);

        _favoritesRepositoryMock
            .Setup(x => x.RemoveAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _favoritesRepositoryMock.Verify(
            x => x.RemoveAsync(userId, tipId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCallRemoveAsync_WhenFavoriteDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var request = new RemoveFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites?)null);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _favoritesRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<UserId>(), It.IsAny<TipId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
