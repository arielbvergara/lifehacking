using Application.Dtos.Dashboard;
using Application.Interfaces;
using Application.UseCases.Dashboard;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Tests.UseCases.Dashboard;

public sealed class GetDashboardUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ITipRepository> _tipRepositoryMock;
    private readonly GetDashboardUseCase _useCase;

    public GetDashboardUseCaseTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _tipRepositoryMock = new Mock<ITipRepository>();
        _useCase = new GetDashboardUseCase(
            _userRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _tipRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDashboardStatistics_WhenDataExists()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = now.AddMonths(-1);
        var lastMonthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var users = new List<global::Domain.Entities.User>
        {
            CreateUser(thisMonthStart.AddDays(5)),
            CreateUser(lastMonthStart.AddDays(10)),
            CreateUser(now.AddMonths(-2))
        };

        var categories = new List<global::Domain.Entities.Category>
        {
            CreateCategory(thisMonthStart.AddDays(3)),
            CreateCategory(lastMonthStart.AddDays(5))
        };

        var tips = new List<global::Domain.Entities.Tip>
        {
            CreateTip(thisMonthStart.AddDays(1)),
            CreateTip(thisMonthStart.AddDays(2)),
            CreateTip(lastMonthStart.AddDays(15))
        };

        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        _tipRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        var request = new GetDashboardRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Users.Total.Should().Be(3);
        result.Value.Users.ThisMonth.Should().Be(1);
        result.Value.Users.LastMonth.Should().Be(1);
        result.Value.Categories.Total.Should().Be(2);
        result.Value.Categories.ThisMonth.Should().Be(1);
        result.Value.Categories.LastMonth.Should().Be(1);
        result.Value.Tips.Total.Should().Be(3);
        result.Value.Tips.ThisMonth.Should().Be(2);
        result.Value.Tips.LastMonth.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnZeroCounts_WhenNoDataExists()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.User>());

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.Category>());

        _tipRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.Tip>());

        var request = new GetDashboardRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Users.Total.Should().Be(0);
        result.Value.Users.ThisMonth.Should().Be(0);
        result.Value.Users.LastMonth.Should().Be(0);
        result.Value.Categories.Total.Should().Be(0);
        result.Value.Categories.ThisMonth.Should().Be(0);
        result.Value.Categories.LastMonth.Should().Be(0);
        result.Value.Tips.Total.Should().Be(0);
        result.Value.Tips.ThisMonth.Should().Be(0);
        result.Value.Tips.LastMonth.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeDeletedCategories_WhenCalculatingStatistics()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeCategory = CreateCategory(thisMonthStart.AddDays(1));
        var deletedCategory = CreateCategory(thisMonthStart.AddDays(2));
        deletedCategory.MarkDeleted();

        var categories = new List<global::Domain.Entities.Category> { activeCategory, deletedCategory };

        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.User>());

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        _tipRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.Tip>());

        var request = new GetDashboardRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Categories.Total.Should().Be(1);
        result.Value.Categories.ThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeDeletedTips_WhenCalculatingStatistics()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeTip = CreateTip(thisMonthStart.AddDays(1));
        var deletedTip = CreateTip(thisMonthStart.AddDays(2));
        deletedTip.MarkDeleted();

        var tips = new List<global::Domain.Entities.Tip> { activeTip, deletedTip };

        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.User>());

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::Domain.Entities.Category>());

        _tipRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        var request = new GetDashboardRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Tips.Total.Should().Be(1);
        result.Value.Tips.ThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var request = new GetDashboardRequest();

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Contain("Failed to retrieve dashboard statistics");
    }

    private static global::Domain.Entities.User CreateUser(DateTime createdAt)
    {
        return global::Domain.Entities.User.FromPersistence(
            UserId.NewId(),
            Email.Create($"user{Guid.NewGuid()}@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create($"auth{Guid.NewGuid()}"),
            "User",
            createdAt,
            null,
            false,
            null);
    }

    private static global::Domain.Entities.Category CreateCategory(DateTime createdAt)
    {
        return global::Domain.Entities.Category.FromPersistence(
            CategoryId.NewId(),
            $"Category {Guid.NewGuid()}",
            createdAt,
            null,
            false,
            null);
    }

    private static global::Domain.Entities.Tip CreateTip(DateTime createdAt)
    {
        return global::Domain.Entities.Tip.FromPersistence(
            TipId.NewId(),
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test Description"),
            new List<TipStep> { TipStep.Create(1, "This is a test step description") },
            CategoryId.NewId(),
            new List<Tag>(),
            null,
            createdAt,
            null,
            false,
            null);
    }
}
