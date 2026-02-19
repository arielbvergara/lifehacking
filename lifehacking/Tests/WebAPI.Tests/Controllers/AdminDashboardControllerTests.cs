using Application.Dtos.Dashboard;
using Application.Exceptions;
using Application.UseCases.Dashboard;
using Domain.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using WebAPI.Controllers;
using Xunit;

namespace WebAPI.Tests.Controllers;

public sealed class AdminDashboardControllerTests
{
    private readonly Mock<GetDashboardUseCase> _useCaseMock;
    private readonly IMemoryCache _memoryCache;
    private readonly AdminDashboardController _controller;

    public AdminDashboardControllerTests()
    {
        _useCaseMock = new Mock<GetDashboardUseCase>(
            Mock.Of<Application.Interfaces.IUserRepository>(),
            Mock.Of<Application.Interfaces.ICategoryRepository>(),
            Mock.Of<Application.Interfaces.ITipRepository>());

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _controller = new AdminDashboardController(_useCaseMock.Object, _memoryCache);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnOk_WhenUseCaseSucceeds()
    {
        // Arrange
        var dashboardResponse = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10, ThisMonth = 5, LastMonth = 3 },
            Categories = new EntityStatistics { Total = 8, ThisMonth = 2, LastMonth = 1 },
            Tips = new EntityStatistics { Total = 20, ThisMonth = 10, LastMonth = 5 }
        };

        _useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse, AppException>.Ok(dashboardResponse));

        // Act
        var result = await _controller.GetDashboard(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(dashboardResponse);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnCachedData_WhenCacheExists()
    {
        // Arrange
        var cachedResponse = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 5, ThisMonth = 2, LastMonth = 1 },
            Categories = new EntityStatistics { Total = 3, ThisMonth = 1, LastMonth = 0 },
            Tips = new EntityStatistics { Total = 10, ThisMonth = 5, LastMonth = 2 }
        };

        _memoryCache.Set("AdminDashboard", cachedResponse, TimeSpan.FromDays(1));

        // Act
        var result = await _controller.GetDashboard(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(cachedResponse);

        // Verify use case was not called
        _useCaseMock.Verify(
            x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDashboard_ShouldCacheResponse_WhenUseCaseSucceeds()
    {
        // Arrange
        var dashboardResponse = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10, ThisMonth = 5, LastMonth = 3 },
            Categories = new EntityStatistics { Total = 8, ThisMonth = 2, LastMonth = 1 },
            Tips = new EntityStatistics { Total = 20, ThisMonth = 10, LastMonth = 5 }
        };

        _useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse, AppException>.Ok(dashboardResponse));

        // Act
        await _controller.GetDashboard(CancellationToken.None);

        // Assert
        var cachedValue = _memoryCache.Get<DashboardResponse>("AdminDashboard");
        cachedValue.Should().NotBeNull();
        cachedValue.Should().BeEquivalentTo(dashboardResponse);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnInternalServerError_WhenUseCaseFails()
    {
        // Arrange
        var error = new InfraException("Database connection failed");

        _useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse, AppException>.Fail(error));

        // Act
        var result = await _controller.GetDashboard(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetDashboard_ShouldNotCacheResponse_WhenUseCaseFails()
    {
        // Arrange
        var error = new InfraException("Database connection failed");

        _useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse, AppException>.Fail(error));

        // Act
        await _controller.GetDashboard(CancellationToken.None);

        // Assert
        var cachedValue = _memoryCache.Get<DashboardResponse>("AdminDashboard");
        cachedValue.Should().BeNull();
    }
}
