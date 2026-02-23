using Domain.Constants;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.ValueObject;

/// <summary>
/// Unit tests for ImageMetadata value object.
/// Tests domain validation rules and value object behavior.
/// </summary>
public sealed class ImageMetadataTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_ShouldReturnImageMetadata_WhenAllPropertiesAreValid()
    {
        // Arrange
        var imageUrl = "https://cdn.example.com/categories/image.jpg";
        var storagePath = "public/categories/2024/01/guid.jpg";
        var fileName = "original-image.jpg";
        var contentType = "image/jpeg";
        var fileSize = 1024 * 500; // 500KB
        var uploadedAt = DateTime.UtcNow;

        // Act
        var result = ImageMetadata.Create(imageUrl, storagePath, fileName, contentType, fileSize, uploadedAt);

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(imageUrl);
        result.ImageStoragePath.Should().Be(storagePath);
        result.OriginalFileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.FileSizeBytes.Should().Be(fileSize);
        result.UploadedAt.Should().Be(uploadedAt);
    }

    [Fact]
    public void Create_ShouldAcceptAllAllowedContentTypes_WhenContentTypeIsValid()
    {
        // Arrange
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        var uploadedAt = DateTime.UtcNow;

        foreach (var contentType in allowedTypes)
        {
            // Act
            var result = ImageMetadata.Create(
                "https://cdn.example.com/image.jpg",
                "public/categories/2024/01/guid.jpg",
                "image.jpg",
                contentType,
                1024,
                uploadedAt);

            // Assert
            result.ContentType.Should().Be(contentType, $"{contentType} should be accepted");
        }
    }

    #endregion

    #region Create - ImageUrl Validation

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageUrlIsEmpty()
    {
        // Act
        var act = () => ImageMetadata.Create(
            string.Empty,
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageUrl")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageUrlIsWhitespace()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "   ",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageUrl")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageUrlIsTooLong()
    {
        // Arrange
        var tooLongUrl = "https://cdn.example.com/" + new string('a', ImageConstants.MaxUrlLength);

        // Act
        var act = () => ImageMetadata.Create(
            tooLongUrl,
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageUrl")
            .WithMessage($"*cannot exceed {ImageConstants.MaxUrlLength} characters*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageUrlIsNotAbsolute()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "relative/path/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageUrl")
            .WithMessage("*must be a valid absolute URL*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageUrlIsInvalidFormat()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "not-a-valid-url",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageUrl")
            .WithMessage("*must be a valid absolute URL*");
    }

    #endregion

    #region Create - ImageStoragePath Validation

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageStoragePathIsEmpty()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            string.Empty,
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageStoragePath")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageStoragePathIsWhitespace()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "   ",
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageStoragePath")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenImageStoragePathIsTooLong()
    {
        // Arrange
        var tooLongPath = new string('a', ImageConstants.MaxStoragePathLength + 1);

        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            tooLongPath,
            "image.jpg",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageStoragePath")
            .WithMessage($"*cannot exceed {ImageConstants.MaxStoragePathLength} characters*");
    }

    #endregion

    #region Create - OriginalFileName Validation

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenOriginalFileNameIsEmpty()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            string.Empty,
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("originalFileName")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenOriginalFileNameIsWhitespace()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "   ",
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("originalFileName")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenOriginalFileNameIsTooLong()
    {
        // Arrange
        var tooLongFileName = new string('a', ImageConstants.MaxFileNameLength + 1) + ".jpg";

        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            tooLongFileName,
            "image/jpeg",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("originalFileName")
            .WithMessage($"*cannot exceed {ImageConstants.MaxFileNameLength} characters*");
    }

    #endregion

    #region Create - ContentType Validation

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenContentTypeIsEmpty()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            string.Empty,
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("contentType")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenContentTypeIsWhitespace()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "   ",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("contentType")
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenContentTypeIsInvalid()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "application/pdf",
            1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("contentType")
            .WithMessage("*Content type must be one of*");
    }

    [Fact]
    public void Create_ShouldBeCaseInsensitive_WhenValidatingContentType()
    {
        // Act
        var result = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "IMAGE/JPEG",
            1024,
            DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull("content type validation should be case-insensitive");
        result.ContentType.Should().Be("IMAGE/JPEG");
    }

    #endregion

    #region Create - FileSizeBytes Validation

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenFileSizeBytesIsZero()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            0,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileSizeBytes")
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenFileSizeBytesIsNegative()
    {
        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            -1024,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileSizeBytes")
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenFileSizeBytesExceedsMaximum()
    {
        // Arrange
        var oversizedFile = ImageConstants.MaxFileSizeBytes + 1;

        // Act
        var act = () => ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            oversizedFile,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileSizeBytes")
            .WithMessage($"*cannot exceed {ImageConstants.MaxFileSizeBytes} bytes*");
    }

    [Fact]
    public void Create_ShouldAcceptMaximumFileSize_WhenFileSizeIsAtLimit()
    {
        // Act
        var result = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            ImageConstants.MaxFileSizeBytes,
            DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.FileSizeBytes.Should().Be(ImageConstants.MaxFileSizeBytes);
    }

    #endregion

    #region Value Object Equality

    [Fact]
    public void Equality_ShouldReturnTrue_WhenAllPropertiesMatch()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image1 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        var image2 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        // Act & Assert
        image1.Should().Be(image2, "value objects with identical properties should be equal");
        (image1 == image2).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_WhenImageUrlDiffers()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image1 = ImageMetadata.Create(
            "https://cdn.example.com/image1.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        var image2 = ImageMetadata.Create(
            "https://cdn.example.com/image2.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        // Act & Assert
        image1.Should().NotBe(image2, "value objects with different URLs should not be equal");
        (image1 != image2).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_WhenFileSizeDiffers()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image1 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        var image2 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            2048,
            uploadedAt);

        // Act & Assert
        image1.Should().NotBe(image2, "value objects with different file sizes should not be equal");
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameValue_WhenObjectsAreEqual()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image1 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        var image2 = ImageMetadata.Create(
            "https://cdn.example.com/image.jpg",
            "public/categories/2024/01/guid.jpg",
            "image.jpg",
            "image/jpeg",
            1024,
            uploadedAt);

        // Act & Assert
        image1.GetHashCode().Should().Be(image2.GetHashCode(), "equal value objects should have the same hash code");
    }

    #endregion
}
