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
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Favorite;

public class MergeFavoritesUseCaseTests
{
    private readonly Mock<IFavoritesRepository> _favoritesRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly MergeFavoritesUseCase _useCase;

    public MergeFavoritesUseCaseTests()
    {
        _favoritesRepositoryMock = new Mock<IFavoritesRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _useCase = new MergeFavoritesUseCase(
            _favoritesRepositoryMock.Object,
            _tipRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var tipIds = new[] { TipId.NewId() };
        var request = new MergeFavoritesRequest(userId, tipIds);

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
    public async Task ExecuteAsync_ShouldReturnSuccessWithZeroCounts_WhenEmptyInputProvided()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var emptyTipIds = Array.Empty<TipId>();
        var request = new MergeFavoritesRequest(userId, emptyTipIds);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(0);
        result.Value.Added.Should().Be(0);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddAllTips_WhenAllTipsAreValidAndNew()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var tipIds = new[] { TipId.NewId(), TipId.NewId(), TipId.NewId() };
        var request = new MergeFavoritesRequest(userId, tipIds);

        var validTips = tipIds.ToDictionary(
            id => id,
            id => (DomainTip)CreateTestTip(id));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validTips);

        _favoritesRepositoryMock
            .Setup(x => x.GetExistingFavoritesAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<TipId>());

        _favoritesRepositoryMock
            .Setup(x => x.AddBatchAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId uid, IReadOnlyCollection<TipId> ids, CancellationToken _) =>
                ids.Select(id => UserFavorites.Create(uid, id)).ToList());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(3);
        result.Value.Added.Should().Be(3);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().BeEmpty();

        _favoritesRepositoryMock.Verify(
            x => x.AddBatchAsync(userId, It.Is<IReadOnlyCollection<TipId>>(ids => ids.Count == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipAllTips_WhenAllTipsAlreadyFavorited()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var tipIds = new[] { TipId.NewId(), TipId.NewId() };
        var request = new MergeFavoritesRequest(userId, tipIds);

        var validTips = tipIds.ToDictionary(
            id => id,
            id => (DomainTip)CreateTestTip(id));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validTips);

        _favoritesRepositoryMock
            .Setup(x => x.GetExistingFavoritesAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tipIds.ToHashSet());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(2);
        result.Value.Added.Should().Be(0);
        result.Value.Skipped.Should().Be(2);
        result.Value.Failed.Should().BeEmpty();

        _favoritesRepositoryMock.Verify(
            x => x.AddBatchAsync(It.IsAny<UserId>(), It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMixOfNewAndExisting_WhenPartialOverlap()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var newTipId1 = TipId.NewId();
        var newTipId2 = TipId.NewId();
        var existingTipId = TipId.NewId();
        var tipIds = new[] { newTipId1, newTipId2, existingTipId };
        var request = new MergeFavoritesRequest(userId, tipIds);

        var validTips = tipIds.ToDictionary(
            id => id,
            id => (DomainTip)CreateTestTip(id));

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validTips);

        _favoritesRepositoryMock
            .Setup(x => x.GetExistingFavoritesAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<TipId> { existingTipId });

        _favoritesRepositoryMock
            .Setup(x => x.AddBatchAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId uid, IReadOnlyCollection<TipId> ids, CancellationToken _) =>
                ids.Select(id => UserFavorites.Create(uid, id)).ToList());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(3);
        result.Value.Added.Should().Be(2);
        result.Value.Skipped.Should().Be(1);
        result.Value.Failed.Should().BeEmpty();

        _favoritesRepositoryMock.Verify(
            x => x.AddBatchAsync(userId, It.Is<IReadOnlyCollection<TipId>>(ids => ids.Count == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReportAllAsFailed_WhenAllTipsInvalid()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var invalidTipIds = new[] { TipId.NewId(), TipId.NewId(), TipId.NewId() };
        var request = new MergeFavoritesRequest(userId, invalidTipIds);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<TipId, DomainTip>()); // No valid tips

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(3);
        result.Value.Added.Should().Be(0);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().HaveCount(3);
        result.Value.Failed.Should().OnlyContain(f => f.ErrorMessage == "Tip not found");

        _favoritesRepositoryMock.Verify(
            x => x.AddBatchAsync(It.IsAny<UserId>(), It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMixOfValidAndInvalid_WhenPartialSuccess()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var validTipId1 = TipId.NewId();
        var validTipId2 = TipId.NewId();
        var invalidTipId = TipId.NewId();
        var tipIds = new[] { validTipId1, validTipId2, invalidTipId };
        var request = new MergeFavoritesRequest(userId, tipIds);

        var validTips = new Dictionary<TipId, DomainTip>
        {
            { validTipId1, CreateTestTip(validTipId1) },
            { validTipId2, CreateTestTip(validTipId2) }
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validTips);

        _favoritesRepositoryMock
            .Setup(x => x.GetExistingFavoritesAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<TipId>());

        _favoritesRepositoryMock
            .Setup(x => x.AddBatchAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId uid, IReadOnlyCollection<TipId> ids, CancellationToken _) =>
                ids.Select(id => UserFavorites.Create(uid, id)).ToList());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(3);
        result.Value.Added.Should().Be(2);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().HaveCount(1);
        result.Value.Failed.First().TipId.Should().Be(invalidTipId.Value);
        result.Value.Failed.First().ErrorMessage.Should().Be("Tip not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeduplicateInput_WhenDuplicateTipIdsProvided()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var tipId = TipId.NewId();
        var duplicateTipIds = new[] { tipId, tipId, tipId }; // Same ID three times
        var request = new MergeFavoritesRequest(userId, duplicateTipIds);

        var validTips = new Dictionary<TipId, DomainTip>
        {
            { tipId, CreateTestTip(tipId) }
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validTips);

        _favoritesRepositoryMock
            .Setup(x => x.GetExistingFavoritesAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<TipId>());

        _favoritesRepositoryMock
            .Setup(x => x.AddBatchAsync(userId, It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId uid, IReadOnlyCollection<TipId> ids, CancellationToken _) =>
                ids.Select(id => UserFavorites.Create(uid, id)).ToList());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalReceived.Should().Be(3); // Original count before deduplication
        result.Value.Added.Should().Be(1); // Only one unique tip added
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().BeEmpty();

        _favoritesRepositoryMock.Verify(
            x => x.AddBatchAsync(userId, It.Is<IReadOnlyCollection<TipId>>(ids => ids.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWrapInfrastructureException_WhenRepositoryThrows()
    {
        // Arrange
        var userId = UserId.NewId();
        var user = CreateTestUser();
        var tipIds = new[] { TipId.NewId() };
        var request = new MergeFavoritesRequest(userId, tipIds);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tipRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<TipId>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.Message.Should().Contain("error occurred while merging favorites");
        result.Error.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    private static DomainUser CreateTestUser()
    {
        return DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("test-auth-id"));
    }

    private static DomainTip CreateTestTip(TipId tipId)
    {
        var categoryId = CategoryId.NewId();
        return DomainTip.FromPersistence(
            tipId,
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test Description"),
            new[] { TipStep.Create(1, "Step 1") },
            categoryId,
            new[] { Tag.Create("test") },
            null,
            DateTime.UtcNow,
            null,
            false,
            null);
    }
}
