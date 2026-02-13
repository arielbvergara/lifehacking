using System.Reflection;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Data.Firestore;
using Xunit;

namespace Infrastructure.Tests;

[Trait("Category", "Unit")]
public sealed class FirestoreTipDataStoreTests
{
    [Fact]
    public void ToDocument_ShouldMapImageFields_WhenTipHasImage()
    {
        // Arrange
        var tipImage = TipImage.Create(
            "https://cdn.example.com/tips/test-image.jpg",
            "tips/test-image.jpg",
            "test-image.jpg",
            "image/jpeg",
            245760,
            DateTime.UtcNow);

        var tip = CreateTestTip(image: tipImage);

        // Act
        var document = InvokeToDocument(tip);

        // Assert
        document.Should().NotBeNull();
        document.ImageUrl.Should().Be(tipImage.ImageUrl);
        document.ImageStoragePath.Should().Be(tipImage.ImageStoragePath);
        document.OriginalFileName.Should().Be(tipImage.OriginalFileName);
        document.ContentType.Should().Be(tipImage.ContentType);
        document.FileSizeBytes.Should().Be(tipImage.FileSizeBytes);
        document.UploadedAt.Should().BeCloseTo(tipImage.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToDocument_ShouldHaveNullImageFields_WhenTipHasNoImage()
    {
        // Arrange
        var tip = CreateTestTip(image: null);

        // Act
        var document = InvokeToDocument(tip);

        // Assert
        document.Should().NotBeNull();
        document.ImageUrl.Should().BeNull();
        document.ImageStoragePath.Should().BeNull();
        document.OriginalFileName.Should().BeNull();
        document.ContentType.Should().BeNull();
        document.FileSizeBytes.Should().BeNull();
        document.UploadedAt.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldReconstructImage_WhenDocumentHasCompleteImageData()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var document = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/complete-image.jpg",
            imageStoragePath: "tips/complete-image.jpg",
            originalFileName: "complete-image.jpg",
            contentType: "image/png",
            fileSizeBytes: 512000,
            uploadedAt: uploadedAt);

        // Act
        var tip = InvokeToDomain(document);

        // Assert
        tip.Should().NotBeNull();
        tip.Image.Should().NotBeNull();
        tip.Image!.ImageUrl.Should().Be("https://cdn.example.com/tips/complete-image.jpg");
        tip.Image.ImageStoragePath.Should().Be("tips/complete-image.jpg");
        tip.Image.OriginalFileName.Should().Be("complete-image.jpg");
        tip.Image.ContentType.Should().Be("image/png");
        tip.Image.FileSizeBytes.Should().Be(512000);
        tip.Image.UploadedAt.Should().BeCloseTo(uploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToDomain_ShouldHaveNullImage_WhenDocumentHasNoImageData()
    {
        // Arrange
        var document = CreateTestDocument(
            imageUrl: null,
            imageStoragePath: null,
            originalFileName: null,
            contentType: null,
            fileSizeBytes: null,
            uploadedAt: null);

        // Act
        var tip = InvokeToDomain(document);

        // Assert
        tip.Should().NotBeNull();
        tip.Image.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldHaveNullImage_WhenDocumentHasPartialImageData()
    {
        // Arrange - Missing ImageUrl
        var documentMissingUrl = CreateTestDocument(
            imageUrl: null,
            imageStoragePath: "tips/partial-image.jpg",
            originalFileName: "partial-image.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 245760,
            uploadedAt: DateTime.UtcNow);

        // Act
        var tipMissingUrl = InvokeToDomain(documentMissingUrl);

        // Assert
        tipMissingUrl.Should().NotBeNull();
        tipMissingUrl.Image.Should().BeNull();

        // Arrange - Missing ImageStoragePath
        var documentMissingPath = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/partial-image.jpg",
            imageStoragePath: null,
            originalFileName: "partial-image.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 245760,
            uploadedAt: DateTime.UtcNow);

        // Act
        var tipMissingPath = InvokeToDomain(documentMissingPath);

        // Assert
        tipMissingPath.Should().NotBeNull();
        tipMissingPath.Image.Should().BeNull();

        // Arrange - Missing OriginalFileName
        var documentMissingFileName = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/partial-image.jpg",
            imageStoragePath: "tips/partial-image.jpg",
            originalFileName: null,
            contentType: "image/jpeg",
            fileSizeBytes: 245760,
            uploadedAt: DateTime.UtcNow);

        // Act
        var tipMissingFileName = InvokeToDomain(documentMissingFileName);

        // Assert
        tipMissingFileName.Should().NotBeNull();
        tipMissingFileName.Image.Should().BeNull();

        // Arrange - Missing ContentType
        var documentMissingContentType = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/partial-image.jpg",
            imageStoragePath: "tips/partial-image.jpg",
            originalFileName: "partial-image.jpg",
            contentType: null,
            fileSizeBytes: 245760,
            uploadedAt: DateTime.UtcNow);

        // Act
        var tipMissingContentType = InvokeToDomain(documentMissingContentType);

        // Assert
        tipMissingContentType.Should().NotBeNull();
        tipMissingContentType.Image.Should().BeNull();

        // Arrange - Missing FileSizeBytes
        var documentMissingFileSize = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/partial-image.jpg",
            imageStoragePath: "tips/partial-image.jpg",
            originalFileName: "partial-image.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: null,
            uploadedAt: DateTime.UtcNow);

        // Act
        var tipMissingFileSize = InvokeToDomain(documentMissingFileSize);

        // Assert
        tipMissingFileSize.Should().NotBeNull();
        tipMissingFileSize.Image.Should().BeNull();

        // Arrange - Missing UploadedAt
        var documentMissingUploadedAt = CreateTestDocument(
            imageUrl: "https://cdn.example.com/tips/partial-image.jpg",
            imageStoragePath: "tips/partial-image.jpg",
            originalFileName: "partial-image.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 245760,
            uploadedAt: null);

        // Act
        var tipMissingUploadedAt = InvokeToDomain(documentMissingUploadedAt);

        // Assert
        tipMissingUploadedAt.Should().NotBeNull();
        tipMissingUploadedAt.Image.Should().BeNull();
    }

    // Helper methods to invoke private static methods using reflection
    private static TipDocument InvokeToDocument(Tip tip)
    {
        var dataStoreType = typeof(FirestoreTipDataStore);
        var method = dataStoreType.GetMethod("MapToDocument", BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("MapToDocument method not found");
        }

        var result = method.Invoke(null, new object[] { tip });
        return (TipDocument)result!;
    }

    private static Tip InvokeToDomain(TipDocument document)
    {
        var dataStoreType = typeof(FirestoreTipDataStore);
        var method = dataStoreType.GetMethod("MapToDomainTip", BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("MapToDomainTip method not found");
        }

        var result = method.Invoke(null, new object[] { document });
        return (Tip)result!;
    }

    private static Tip CreateTestTip(TipImage? image = null)
    {
        var categoryId = CategoryId.Create(Guid.NewGuid());
        var tipTitle = TipTitle.Create("Test Tip");
        var tipDescription = TipDescription.Create("This is a test tip description for testing purposes.");
        var steps = new[]
        {
            TipStep.Create(1, "First step of the tip"),
            TipStep.Create(2, "Second step of the tip")
        };
        var tipTags = new[] { Tag.Create("test") };

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags, null, image);
    }

    private static TipDocument CreateTestDocument(
        string? imageUrl,
        string? imageStoragePath,
        string? originalFileName,
        string? contentType,
        long? fileSizeBytes,
        DateTime? uploadedAt)
    {
        return new TipDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Tip",
            Description = "This is a test tip description for testing purposes.",
            Steps = new List<TipStepDocument>
            {
                new() { StepNumber = 1, Description = "First step of the tip" },
                new() { StepNumber = 2, Description = "Second step of the tip" }
            },
            CategoryId = Guid.NewGuid().ToString(),
            Tags = new List<string> { "test" },
            VideoUrl = null,
            ImageUrl = imageUrl,
            ImageStoragePath = imageStoragePath,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = uploadedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false,
            DeletedAt = null
        };
    }
}
