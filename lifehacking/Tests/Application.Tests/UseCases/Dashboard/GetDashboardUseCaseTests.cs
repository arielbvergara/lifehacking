using Application.Dtos.Dashboard;
using Application.Interfaces;
using Application.UseCases.Dashboard;
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
        var today = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0, DateTimeKind.Utc);
        var yesterday = today.AddDays(-1);
        var thisWeekStart = GetMondayOfWeek(now);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = now.AddMonths(-1);
        var lastMonthStart = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thisYearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastYearStart = new DateTime(now.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var users = new List<global::Domain.Entities.User>
        {
            CreateUser(now.AddHours(-2)),                 // ThisDay, ThisWeek, ThisMonth, ThisYear
            CreateUser(yesterday.AddHours(5)),            // LastDay, possibly ThisWeek, ThisMonth, ThisYear
            CreateUser(thisWeekStart.AddDays(1)),         // ThisWeek, ThisMonth, ThisYear
            CreateUser(lastWeekStart.AddDays(3)),         // LastWeek, possibly ThisMonth, ThisYear
            CreateUser(thisMonthStart.AddDays(5)),        // ThisMonth, ThisYear
            CreateUser(lastMonthStart.AddDays(10)),       // LastMonth, possibly ThisYear
            CreateUser(thisYearStart.AddDays(30)),        // ThisYear
            CreateUser(lastYearStart.AddDays(100))        // LastYear
        };

        var categories = new List<global::Domain.Entities.Category>
        {
            CreateCategory(now.AddHours(-1)),
            CreateCategory(thisMonthStart.AddDays(3)),
            CreateCategory(lastMonthStart.AddDays(5))
        };

        var tips = new List<global::Domain.Entities.Tip>
        {
            CreateTip(now.AddHours(-3)),
            CreateTip(yesterday.AddHours(10)),
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
        
        // Users assertions - verify totals and that new properties exist
        result.Value!.Users.Total.Should().Be(8);
        result.Value.Users.ThisDay.Should().BeGreaterOrEqualTo(1);
        result.Value.Users.LastDay.Should().BeGreaterOrEqualTo(0);
        result.Value.Users.ThisWeek.Should().BeGreaterOrEqualTo(1);
        result.Value.Users.LastWeek.Should().BeGreaterOrEqualTo(0);
        result.Value.Users.ThisMonth.Should().BeGreaterOrEqualTo(1);
        result.Value.Users.LastMonth.Should().BeGreaterOrEqualTo(0);
        result.Value.Users.ThisYear.Should().BeGreaterOrEqualTo(1);
        result.Value.Users.LastYear.Should().Be(1);
        
        // Categories assertions
        result.Value.Categories.Total.Should().Be(3);
        result.Value.Categories.ThisMonth.Should().BeGreaterOrEqualTo(1);
        
        // Tips assertions
        result.Value.Tips.Total.Should().Be(5);
        result.Value.Tips.ThisDay.Should().BeGreaterOrEqualTo(1);
        result.Value.Tips.ThisMonth.Should().BeGreaterOrEqualTo(1);
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
        
        // Users assertions
        result.Value!.Users.Total.Should().Be(0);
        result.Value.Users.ThisDay.Should().Be(0);
        result.Value.Users.LastDay.Should().Be(0);
        result.Value.Users.ThisWeek.Should().Be(0);
        result.Value.Users.LastWeek.Should().Be(0);
        result.Value.Users.ThisMonth.Should().Be(0);
        result.Value.Users.LastMonth.Should().Be(0);
        result.Value.Users.ThisYear.Should().Be(0);
        result.Value.Users.LastYear.Should().Be(0);
        
        // Categories assertions
        result.Value.Categories.Total.Should().Be(0);
        result.Value.Categories.ThisDay.Should().Be(0);
        result.Value.Categories.LastDay.Should().Be(0);
        result.Value.Categories.ThisWeek.Should().Be(0);
        result.Value.Categories.LastWeek.Should().Be(0);
        result.Value.Categories.ThisMonth.Should().Be(0);
        result.Value.Categories.LastMonth.Should().Be(0);
        result.Value.Categories.ThisYear.Should().Be(0);
        result.Value.Categories.LastYear.Should().Be(0);
        
        // Tips assertions
        result.Value.Tips.Total.Should().Be(0);
        result.Value.Tips.ThisDay.Should().Be(0);
        result.Value.Tips.LastDay.Should().Be(0);
        result.Value.Tips.ThisWeek.Should().Be(0);
        result.Value.Tips.LastWeek.Should().Be(0);
        result.Value.Tips.ThisMonth.Should().Be(0);
        result.Value.Tips.LastMonth.Should().Be(0);
        result.Value.Tips.ThisYear.Should().Be(0);
        result.Value.Tips.LastYear.Should().Be(0);
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

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateWeekStatisticsCorrectly_WhenWeekStartsOnMonday()
    {
        // Arrange - Use relative dates from now to ensure test works regardless of when it runs
        var now = DateTime.UtcNow;
        var thisMonday = GetMondayOfWeek(now);
        var lastMonday = thisMonday.AddDays(-7);
        var lastSunday = lastMonday.AddDays(6);

        var users = new List<global::Domain.Entities.User>
        {
            CreateUser(thisMonday.AddHours(10)),          // This week (Monday)
            CreateUser(now.AddHours(-2)),                 // This week (current time)
            CreateUser(lastMonday.AddHours(5)),           // Last week (Monday)
            CreateUser(lastSunday.AddHours(12)),          // Last week (Sunday)
            CreateUser(lastMonday.AddDays(-1))            // Before last week
        };

        _userRepositoryMock
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

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
        result.Value!.Users.Total.Should().Be(5);
        result.Value.Users.ThisWeek.Should().Be(2, "Monday and current time should be in this week");
        result.Value.Users.LastWeek.Should().Be(2, "Last Monday and Sunday should be in last week");
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

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var monday = date.AddDays(-daysFromMonday);
        return new DateTime(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
