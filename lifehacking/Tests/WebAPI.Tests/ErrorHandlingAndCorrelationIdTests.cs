using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.ErrorHandling;
using WebAPI.Filters;
using WebAPI.Middleware;
using Xunit;

namespace WebAPI.Tests;

public class ErrorHandlingAndCorrelationIdTests
{
    [Fact]
    public void OnException_ShouldReturnStandardizedErrorResponse_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id";
        httpContext.Request.Path = "/test-path";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("boom")
        };

        var logger = NullLogger<GlobalExceptionFilter>.Instance;
        var filter = new GlobalExceptionFilter(logger);

        // Act
        filter.OnException(exceptionContext);

        // Assert
        var objectResult = exceptionContext.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var payload = objectResult.Value as ApiErrorResponse;
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        payload.Type.Should().Be(ErrorResponseTypes.GenericErrorType);
        payload.Title.Should().Be(ErrorResponseTitles.GenericErrorTitle);
        payload.Detail.Should().Be(ErrorResponseMapper.GenericClientSafeServerErrorDetail);
        payload.Instance.Should().Be("/test-path");
        payload.CorrelationId.Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrelationIdHeader_WhenHeaderIsMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        var middleware = new CorrelationIdMiddleware(Next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.TryGetValue(CorrelationIdDefaults.CorrelationIdHeaderName, out var headerValues)
            .Should().BeTrue();

        var headerValue = headerValues.ToString();
        headerValue.Should().NotBeNullOrWhiteSpace();
        context.TraceIdentifier.Should().Be(headerValue);
        return;

        Task Next(HttpContext _) => Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHonorExistingCorrelationIdHeader_WhenHeaderIsPresent()
    {
        // Arrange
        const string existingCorrelationId = "existing-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = existingCorrelationId;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.TraceIdentifier.Should().Be(existingCorrelationId);
        context.Response.Headers[CorrelationIdDefaults.CorrelationIdHeaderName].ToString()
            .Should().Be(existingCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRejectCorrelationId_WhenHeaderContainsSpecialCharacters()
    {
        // Arrange — attempt log injection via newline characters
        const string maliciousCorrelationId = "legit-id\r\nInjected-Header: evil";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = maliciousCorrelationId;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — a new GUID should have been generated instead
        context.TraceIdentifier.Should().NotBe(maliciousCorrelationId);
        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue(
            "a fresh GUID should be generated when the client-provided value is invalid");
    }

    [Fact]
    public async Task InvokeAsync_ShouldRejectCorrelationId_WhenHeaderExceedsMaxLength()
    {
        // Arrange — oversized value that could degrade logging infrastructure
        var oversizedCorrelationId = new string('a', CorrelationIdDefaults.MaxCorrelationIdLength + 1);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = oversizedCorrelationId;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.TraceIdentifier.Should().NotBe(oversizedCorrelationId);
        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue(
            "a fresh GUID should be generated when the client-provided value is too long");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAcceptCorrelationId_WhenHeaderContainsSafeCharacters()
    {
        // Arrange — valid correlation ID with safe characters
        const string safeCorrelationId = "abc-123_DEF.456:789";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = safeCorrelationId;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.TraceIdentifier.Should().Be(safeCorrelationId);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("id with spaces")]
    [InlineData("id\twith\ttabs")]
    [InlineData("id;DROP TABLE logs;")]
    public async Task InvokeAsync_ShouldRejectCorrelationId_WhenHeaderContainsUnsafePatterns(string unsafeValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = unsafeValue;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert — the unsafe value should have been replaced
        context.TraceIdentifier.Should().NotBe(unsafeValue);
        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue();
    }
}
