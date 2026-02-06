using Application.Dtos.Favorite;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Favorite;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;
using DomainUser = Domain.Entities.User;

namespace Application.Tests.UseCases.Favorite;

public class AddFavoriteUseCaseTests
{
    private readonly Mock<IFavoritesRepository> _favoritesRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly AddFavoriteUseCase _useCase;

    public AddFavoriteUseCaseTests()
    {
        _favoritesRepositoryMock = new Mock<IFavoritesRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();

        _useCase = new AddFavoriteUseCase(
            _favoritesRepositoryMock.Object,
            _tipRepositoryMock.Object,
            _userRepositoryMock.Object,
            _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var tip = DomainTip.Create(
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test Description"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryId);

        var category = DomainCategory.Create("Test Category");

        var request = new AddFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites?)null);

        _favoritesRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserFavorites>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites f, CancellationToken _) => f);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TipId.Should().Be(tipId.Value);
        result.Value.TipDetails.Should().NotBeNull();
        result.Value.TipDetails.Title.Should().Be("Test Tip");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var request = new AddFavoriteRequest(userId, tipId);

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
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenTipDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var request = new AddFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain(tipId.Value.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnConflictException_WhenFavoriteAlreadyExists()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var tip = DomainTip.Create(
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test Description"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryId);

        var existingFavorite = UserFavorites.Create(userId, tipId);

        var request = new AddFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFavorite);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        result.Error!.Message.Should().Contain("already in user's favorites");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _useCase.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallRepositoryAddAsync_WhenValidRequest()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var tip = DomainTip.Create(
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test Description"),
            new[] { TipStep.Create(1, "Step 1 description") },
            categoryId);

        var category = DomainCategory.Create("Test Category");

        var request = new AddFavoriteRequest(userId, tipId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        _favoritesRepositoryMock
            .Setup(x => x.GetByUserAndTipAsync(userId, tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites?)null);

        _favoritesRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserFavorites>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserFavorites f, CancellationToken _) => f);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _favoritesRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<UserFavorites>(f => f.UserId == userId && f.TipId == tipId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
