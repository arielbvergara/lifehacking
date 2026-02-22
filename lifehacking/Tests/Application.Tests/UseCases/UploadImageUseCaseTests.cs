using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases;
using Domain.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.UseCases;

/// <summary>
/// Unit tests for UploadImageUseCase.
/// Tests validation, error handling, and integration with IImageStorageService.
/// </summary>
public sealed class UploadImageUseCaseTests
{
    private readonly Mock<IImageStorageService> _mockImageStorageService;
    private readonly Mock<ILogger<UploadImageUseCase>> _mockLogger;
    private readonly UploadImageUseCase _useCase;

    public UploadImageUseCaseTests()
    {
        _mockImageStorageService = new Mock<IImageStorageService>();
        _mockLogger = new Mock<ILogger<UploadImageUseCase>>();
        _useCase = new UploadImageUseCase(_mockImageStorageService.Object, _mockLogger.Object);
    }

    #region Success Cases

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidJpegImageProvided()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        using var stream = new MemoryStream(jpegMagicBytes);
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        var fileSize = 1024 * 500; // 500KB

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.jpg",
            "https://cdn.example.com/public/categories/2025/01/guid.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, fileName, contentType, fileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ImageUrl.Should().Be(expectedStorageResult.PublicUrl);
        result.Value.ImageStoragePath.Should().Be(expectedStorageResult.StoragePath);
        result.Value.OriginalFileName.Should().Be(fileName);
        result.Value.ContentType.Should().Be(contentType);
        result.Value.FileSizeBytes.Should().Be(fileSize);
        result.Value.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockImageStorageService.Verify(
            x => x.UploadAsync(stream, fileName, contentType, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidPngImageProvided()
    {
        // Arrange
        var pngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngMagicBytes);
        var fileName = "test-image.png";
        var contentType = "image/png";
        var fileSize = 1024 * 300;

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.png",
            "https://cdn.example.com/public/categories/2025/01/guid.png");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, fileName, contentType, fileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be(contentType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidGifImageProvided()
    {
        // Arrange
        var gifMagicBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
        using var stream = new MemoryStream(gifMagicBytes);
        var fileName = "test-image.gif";
        var contentType = "image/gif";
        var fileSize = 1024 * 200;

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.gif",
            "https://cdn.example.com/public/categories/2025/01/guid.gif");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, fileName, contentType, fileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be(contentType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenValidWebpImageProvided()
    {
        // Arrange
        var webpMagicBytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(webpMagicBytes);
        var fileName = "test-image.webp";
        var contentType = "image/webp";
        var fileSize = 1024 * 400;

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.webp",
            "https://cdn.example.com/public/categories/2025/01/guid.webp");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), contentType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, fileName, contentType, fileSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be(contentType);
    }

    #endregion

    #region Validation Error Cases

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenFileStreamIsNull()
    {
        // Act
        var result = await _useCase.ExecuteAsync(null!, "test.jpg", "image/jpeg", 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var validationError = result.Error as ValidationException;
        validationError!.Errors.Should().ContainKey("File");
        validationError.Errors["File"].Should().Contain("File is required");

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenFileSizeExceedsMaximum()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        using var stream = new MemoryStream(jpegMagicBytes);
        var oversizedFile = ImageConstants.MaxFileSizeBytes + 1;

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", oversizedFile);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var validationError = result.Error as ValidationException;
        validationError!.Errors.Should().ContainKey("File");
        validationError.Errors["File"].Should().Contain(e => e.Contains("cannot exceed") && e.Contains("MB"));

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenContentTypeIsInvalid()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        using var stream = new MemoryStream(jpegMagicBytes);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.pdf", "application/pdf", 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var validationError = result.Error as ValidationException;
        validationError!.Errors.Should().ContainKey("File");
        validationError.Errors["File"].Should().Contain(e => e.Contains("Content type must be one of"));

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenMagicBytesDoNotMatch()
    {
        // Arrange - PNG magic bytes but claiming to be JPEG
        var pngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngMagicBytes);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "fake.jpg", "image/jpeg", 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var validationError = result.Error as ValidationException;
        validationError!.Errors.Should().ContainKey("File");
        validationError.Errors["File"].Should().Contain("File format does not match the declared content type");

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_WhenContentTypeIsNull()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        using var stream = new MemoryStream(jpegMagicBytes);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", null!, 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var validationError = result.Error as ValidationException;
        validationError!.Errors.Should().ContainKey("File");

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Filename Sanitization

    [Fact]
    public async Task ExecuteAsync_ShouldSanitizeFileName_WhenFileNameContainsInvalidCharacters()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);
        var maliciousFileName = "../../../etc/passwd.jpg";
        var expectedSanitizedName = "etcpasswd.jpg";

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.jpg",
            "https://cdn.example.com/public/categories/2025/01/guid.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), expectedSanitizedName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, maliciousFileName, "image/jpeg", 1024);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalFileName.Should().Be(expectedSanitizedName, "filename should be sanitized");

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), expectedSanitizedName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Infrastructure Error Cases

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfraException_WhenStorageServiceFails()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InfraException("S3", "S3 upload failed"));

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.Message.Should().Contain("S3 upload failed");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInfraException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", 1024);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InfraException>();
        result.Error!.Message.Should().Contain("unexpected error");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExecuteAsync_ShouldLogInformation_WhenUploadSucceeds()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);
        var fileName = "test-image.jpg";

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.jpg",
            "https://cdn.example.com/public/categories/2025/01/guid.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        await _useCase.ExecuteAsync(stream, fileName, "image/jpeg", 1024);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Uploading categories image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully uploaded categories image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogError_WhenUploadFails()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InfraException("S3", "Upload failed"));

        // Act
        await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", 1024);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Application error during categories image upload")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteAsync_ShouldSetUploadedAtToUtcNow_WhenUploadSucceeds()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);
        var beforeUpload = DateTime.UtcNow;

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.jpg",
            "https://cdn.example.com/public/categories/2025/01/guid.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", 1024);
        var afterUpload = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UploadedAt.Should().BeOnOrAfter(beforeUpload);
        result.Value.UploadedAt.Should().BeOnOrBefore(afterUpload);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPublicUrlAndStoragePath_WhenUploadSucceeds()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/abc-123.jpg",
            "https://d123.cloudfront.net/public/categories/2025/01/abc-123.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", "image/jpeg", 1024);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ImageUrl.Should().Be(expectedStorageResult.PublicUrl);
        result.Value.ImageStoragePath.Should().Be(expectedStorageResult.StoragePath);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeContentType_WhenContentTypeHasMixedCase()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);
        var mixedCaseContentType = "IMAGE/JPEG";

        var expectedStorageResult = new ImageStorageResult(
            "public/categories/2025/01/guid.jpg",
            "https://cdn.example.com/public/categories/2025/01/guid.jpg");

        _mockImageStorageService
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageResult);

        // Act
        var result = await _useCase.ExecuteAsync(stream, "test.jpg", mixedCaseContentType, 1024);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("image/jpeg", "content type should be normalized to lowercase");

        _mockImageStorageService.Verify(
            x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
