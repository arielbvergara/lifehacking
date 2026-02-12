using Amazon.S3;
using Amazon.S3.Model;
using Application.Exceptions;
using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Configuration;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Storage;

/// <summary>
/// Unit tests for S3ImageStorageService.
/// Tests AWS S3 integration with mocked IAmazonS3 client.
/// </summary>
public sealed class S3ImageStorageServiceTests
{
    private readonly Mock<IAmazonS3> _mockS3Client;
    private readonly Mock<ILogger<S3ImageStorageService>> _mockLogger;
    private readonly IOptions<AwsS3Options> _s3Options;
    private readonly IOptions<AwsCloudFrontOptions> _cloudFrontOptions;
    private readonly S3ImageStorageService _service;

    public S3ImageStorageServiceTests()
    {
        _mockS3Client = new Mock<IAmazonS3>();
        _mockLogger = new Mock<ILogger<S3ImageStorageService>>();
        
        _s3Options = Options.Create(new AwsS3Options
        {
            BucketName = "test-lifehacking-images",
            Region = "eu-central-1"
        });

        _cloudFrontOptions = Options.Create(new AwsCloudFrontOptions
        {
            Domain = "d1234567890abc.cloudfront.net"
        });

        _service = new S3ImageStorageService(
            _mockS3Client.Object,
            _s3Options,
            _cloudFrontOptions,
            _mockLogger.Object);
    }

    #region Success Cases

    [Fact]
    public async Task UploadAsync_ShouldUploadToS3_WhenValidImageProvided()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        var expectedResponse = new PutObjectResponse
        {
            ETag = "test-etag-123"
        };

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StoragePath.Should().StartWith("public/categories/");
        result.StoragePath.Should().EndWith(".jpg");
        result.PublicUrl.Should().StartWith($"https://{_cloudFrontOptions.Value.Domain}/public/categories/");

        _mockS3Client.Verify(
            x => x.PutObjectAsync(
                It.Is<PutObjectRequest>(req =>
                    req.BucketName == _s3Options.Value.BucketName &&
                    req.ContentType == contentType &&
                    req.ServerSideEncryptionMethod == ServerSideEncryptionMethod.AES256),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ShouldGenerateUniqueStoragePath_WhenCalled()
    {
        // Arrange
        using var stream1 = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        using var stream2 = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        var result1 = await _service.UploadAsync(stream1, fileName, contentType, CancellationToken.None);
        var result2 = await _service.UploadAsync(stream2, fileName, contentType, CancellationToken.None);

        // Assert
        result1.StoragePath.Should().NotBe(result2.StoragePath, "each upload should generate a unique GUID-based path");
    }

    [Fact]
    public async Task UploadAsync_ShouldGenerateCloudFrontUrl_WhenUploadSucceeds()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        result.PublicUrl.Should().StartWith($"https://{_cloudFrontOptions.Value.Domain}/");
        result.PublicUrl.Should().Contain(result.StoragePath);
    }

    [Fact]
    public async Task UploadAsync_ShouldSetServerSideEncryption_WhenUploading()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        PutObjectRequest? capturedRequest = null;
        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.ServerSideEncryptionMethod.Should().Be(ServerSideEncryptionMethod.AES256,
            "all uploads should use AES256 server-side encryption");
    }

    [Fact]
    public async Task UploadAsync_ShouldSetCacheControlHeader_WhenUploading()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        PutObjectRequest? capturedRequest = null;
        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.CacheControl.Should().Be("public, max-age=31536000",
            "immutable images should have 1 year cache control");
    }

    [Fact]
    public async Task UploadAsync_ShouldExtractFileExtension_WhenGeneratingStoragePath()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var fileName = "test-image.png";
        var contentType = "image/png";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        result.StoragePath.Should().EndWith(".png", "file extension should be extracted from original filename");
    }

    [Fact]
    public async Task UploadAsync_ShouldUseDefaultExtension_WhenFileHasNoExtension()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image-no-extension";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        result.StoragePath.Should().EndWith(".jpg", "default extension should be .jpg when no extension is provided");
    }

    [Fact]
    public async Task UploadAsync_ShouldOrganizeByYearAndMonth_WhenGeneratingStoragePath()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        var now = DateTime.UtcNow;
        var expectedYearMonth = $"{now.Year}/{now.Month:D2}";
        result.StoragePath.Should().Contain(expectedYearMonth,
            "storage path should be organized by year and zero-padded month");
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task UploadAsync_ShouldThrowInfraException_WhenS3UploadFails()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 service unavailable"));

        // Act
        var act = async () => await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InfraException>()
            .WithMessage("*Failed to upload image to storage*");
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentNullException_WhenFileStreamIsNull()
    {
        // Act
        var act = async () => await _service.UploadAsync(null!, "test.jpg", "image/jpeg", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("fileStream");
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentException_WhenFileNameIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = async () => await _service.UploadAsync(stream, string.Empty, "image/jpeg", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("originalFileName");
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentException_WhenFileNameIsWhitespace()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = async () => await _service.UploadAsync(stream, "   ", "image/jpeg", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("originalFileName");
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentException_WhenContentTypeIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = async () => await _service.UploadAsync(stream, "test.jpg", string.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("contentType");
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentException_WhenContentTypeIsWhitespace()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = async () => await _service.UploadAsync(stream, "test.jpg", "   ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("contentType");
    }

    [Fact]
    public async Task UploadAsync_ShouldWrapUnexpectedException_WhenNonS3ExceptionOccurs()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var act = async () => await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InfraException>()
            .WithMessage("*unexpected error*");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task UploadAsync_ShouldLogInformation_WhenUploadSucceeds()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Uploading image to S3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully uploaded image to S3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ShouldLogError_WhenUploadFails()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 error"));

        // Act
        try
        {
            await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("S3 upload failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UploadAsync_ShouldHandleMultipleFileExtensions_WhenGeneratingStoragePath()
    {
        // Arrange
        var testCases = new[]
        {
            ("image.jpg", ".jpg"),
            ("image.png", ".png"),
            ("image.gif", ".gif"),
            ("image.webp", ".webp"),
            ("image.JPEG", ".JPEG"),
            ("my.image.jpg", ".jpg")
        };

        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        foreach (var (fileName, expectedExtension) in testCases)
        {
            using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

            // Act
            var result = await _service.UploadAsync(stream, fileName, "image/jpeg", CancellationToken.None);

            // Assert
            result.StoragePath.Should().EndWith(expectedExtension, $"extension should be extracted correctly from {fileName}");
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldUseBucketNameFromOptions_WhenUploading()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";

        PutObjectRequest? capturedRequest = null;
        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        await _service.UploadAsync(stream, fileName, contentType, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_s3Options.Value.BucketName);
    }

    [Fact]
    public async Task UploadAsync_ShouldRespectCancellationToken_WhenProvided()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        var cts = new CancellationTokenSource();

        CancellationToken capturedToken = default;
        _mockS3Client
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, ct) => capturedToken = ct)
            .ReturnsAsync(new PutObjectResponse { ETag = "test-etag" });

        // Act
        await _service.UploadAsync(stream, fileName, contentType, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token, "cancellation token should be passed to S3 client");
    }

    #endregion
}
