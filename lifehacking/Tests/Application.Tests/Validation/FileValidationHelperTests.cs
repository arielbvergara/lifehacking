using Application.Validation;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Validation;

/// <summary>
/// Unit tests for FileValidationHelper.
/// Tests magic byte validation and filename sanitization for security.
/// </summary>
public sealed class FileValidationHelperTests
{
    #region ValidateMagicBytes Tests

    [Fact]
    public void ValidateMagicBytes_ShouldReturnTrue_WhenJpegMagicBytesMatch()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        using var stream = new MemoryStream(jpegMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/jpeg");

        // Assert
        result.Should().BeTrue("JPEG magic bytes should match");
        stream.Position.Should().Be(0, "stream position should be reset after validation");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldReturnTrue_WhenPngMagicBytesMatch()
    {
        // Arrange
        var pngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        using var stream = new MemoryStream(pngMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/png");

        // Assert
        result.Should().BeTrue("PNG magic bytes should match");
        stream.Position.Should().Be(0, "stream position should be reset after validation");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldReturnTrue_WhenGifMagicBytesMatch()
    {
        // Arrange
        var gifMagicBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
        using var stream = new MemoryStream(gifMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/gif");

        // Assert
        result.Should().BeTrue("GIF magic bytes should match");
        stream.Position.Should().Be(0, "stream position should be reset after validation");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldReturnTrue_WhenWebpMagicBytesMatch()
    {
        // Arrange
        var webpMagicBytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00 }; // RIFF header
        using var stream = new MemoryStream(webpMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/webp");

        // Assert
        result.Should().BeTrue("WebP magic bytes should match");
        stream.Position.Should().Be(0, "stream position should be reset after validation");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldReturnFalse_WhenMagicBytesDoNotMatch()
    {
        // Arrange - PNG magic bytes but claiming to be JPEG
        var pngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/jpeg");

        // Assert
        result.Should().BeFalse("magic bytes should not match when content type is incorrect");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldReturnFalse_WhenStreamIsTooShort()
    {
        // Arrange - Only 2 bytes when JPEG needs at least 3
        var shortBytes = new byte[] { 0xFF, 0xD8 };
        using var stream = new MemoryStream(shortBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "image/jpeg");

        // Assert
        result.Should().BeFalse("validation should fail when stream is too short");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldResetStreamPosition_WhenValidationCompletes()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        using var stream = new MemoryStream(jpegMagicBytes);
        stream.Position = 0;

        // Act
        FileValidationHelper.ValidateMagicBytes(stream, "image/jpeg");

        // Assert
        stream.Position.Should().Be(0, "stream position must be reset to allow subsequent reads");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // Act
        var act = () => FileValidationHelper.ValidateMagicBytes(null!, "image/jpeg");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldThrowArgumentNullException_WhenContentTypeIsNull()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = () => FileValidationHelper.ValidateMagicBytes(stream, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contentType");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldThrowArgumentNullException_WhenContentTypeIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = () => FileValidationHelper.ValidateMagicBytes(stream, string.Empty);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contentType");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldThrowArgumentException_WhenContentTypeIsUnsupported()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var act = () => FileValidationHelper.ValidateMagicBytes(stream, "application/pdf");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("contentType")
            .WithMessage("*Unsupported content type*");
    }

    [Fact]
    public void ValidateMagicBytes_ShouldBeCaseInsensitive_WhenValidatingContentType()
    {
        // Arrange
        var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var stream = new MemoryStream(jpegMagicBytes);

        // Act
        var result = FileValidationHelper.ValidateMagicBytes(stream, "IMAGE/JPEG");

        // Assert
        result.Should().BeTrue("content type validation should be case-insensitive");
    }

    #endregion

    #region SanitizeFileName Tests

    [Fact]
    public void SanitizeFileName_ShouldRemovePathTraversalSequences_WhenPresent()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd.jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(maliciousFileName);

        // Assert
        result.Should().Be("etcpasswd.jpg", "path traversal sequences should be removed");
    }

    [Fact]
    public void SanitizeFileName_ShouldRemovePathSeparators_WhenPresent()
    {
        // Arrange
        var fileNameWithPaths = "folder/subfolder\\image.jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(fileNameWithPaths);

        // Assert
        result.Should().Be("foldersubfolderimage.jpg", "path separators should be removed");
    }

    [Fact]
    public void SanitizeFileName_ShouldRemoveNullBytes_WhenPresent()
    {
        // Arrange
        var fileNameWithNullBytes = "image\0.jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(fileNameWithNullBytes);

        // Assert
        result.Should().Be("image.jpg", "null bytes should be removed");
    }

    [Fact]
    public void SanitizeFileName_ShouldTrimWhitespace_WhenPresent()
    {
        // Arrange
        var fileNameWithWhitespace = "  image.jpg  ";

        // Act
        var result = FileValidationHelper.SanitizeFileName(fileNameWithWhitespace);

        // Assert
        result.Should().Be("image.jpg", "leading and trailing whitespace should be trimmed");
    }

    [Fact]
    public void SanitizeFileName_ShouldTruncateToMaxLength_WhenFileNameIsTooLong()
    {
        // Arrange
        var longFileName = new string('a', 300) + ".jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(longFileName);

        // Assert
        result.Length.Should().BeLessOrEqualTo(255, "filename should be truncated to 255 characters");
    }

    [Fact]
    public void SanitizeFileName_ShouldPreserveExtension_WhenTruncating()
    {
        // Arrange
        var longFileName = new string('a', 300) + ".jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(longFileName);

        // Assert
        result.Should().EndWith(".jpg", "file extension should be preserved when truncating");
        result.Length.Should().BeLessOrEqualTo(255);
    }

    [Fact]
    public void SanitizeFileName_ShouldThrowArgumentNullException_WhenFileNameIsNull()
    {
        // Act
        var act = () => FileValidationHelper.SanitizeFileName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileName");
    }

    [Fact]
    public void SanitizeFileName_ShouldHandleValidFileName_WhenNoSanitizationNeeded()
    {
        // Arrange
        var validFileName = "my-image-file.jpg";

        // Act
        var result = FileValidationHelper.SanitizeFileName(validFileName);

        // Assert
        result.Should().Be("my-image-file.jpg", "valid filenames should pass through unchanged");
    }

    [Fact]
    public void SanitizeFileName_ShouldHandleMultipleThreats_WhenCombinedAttackAttempted()
    {
        // Arrange
        var maliciousFileName = "  ../../../folder/sub\\file\0name.jpg  ";

        // Act
        var result = FileValidationHelper.SanitizeFileName(maliciousFileName);

        // Assert
        result.Should().Be("foldersubfilename.jpg", "all security threats should be removed");
    }

    [Fact]
    public void SanitizeFileName_ShouldHandleFileWithoutExtension_WhenTruncating()
    {
        // Arrange
        var longFileNameNoExtension = new string('a', 300);

        // Act
        var result = FileValidationHelper.SanitizeFileName(longFileNameNoExtension);

        // Assert
        result.Length.Should().Be(255, "filename without extension should be truncated to 255 characters");
    }

    #endregion
}
