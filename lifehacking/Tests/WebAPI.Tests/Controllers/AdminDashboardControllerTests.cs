using Application.Dtos.Dashboard;
using Application.Exceptions;
using Application.UseCases.Dashboard;
using Domain.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebAPI.Controllers;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests.Controllers;

public sealed class AdminDashboardControllerTests
{
    private readonly Mock<GetDashboardUseCase> _useCaseMock;
    private readonly AdminDashboardController _controller;

    public AdminDashboardControllerTests()
    {
        _useCaseMock = new Mock<GetDashboardUseCase>(
            Mock.Of<Application.Interfaces.IUserRepository>(),
            Mock.Of<Application.Interfaces.ICategoryRepository>(),
            Mock.Of<Application.Interfaces.ITipRepository>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>());

        var logger = NullLogger<AdminDashboardController>.Instance;
        _controller = new AdminDashboardController(_useCaseMock.Object, logger);

        // Provide an HttpContext so that ErrorResponseMapper.ToActionResult can resolve
        // request path and correlation id without throwing.
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
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
    public async Task GetDashboard_ShouldReturnInternalServerError_WhenUseCaseFails()
    {
        // Arrange
        var error = new InfraException("Database connection failed");

        _useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<GetDashboardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardResponse, AppException>.Fail(error));

        // Act
        var result = await _controller.GetDashboard(CancellationToken.None);

        // Assert – the response must use the standard RFC 7807 envelope and must
        // NOT leak the raw exception message to the client.
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var errorResponse = objectResult.Value as ApiErrorResponse;
        errorResponse.Should().NotBeNull();
        errorResponse!.Detail.Should().Be(ErrorResponseMapper.GenericClientSafeServerErrorDetail,
            "infrastructure error details must not be exposed to clients");
    }
}
