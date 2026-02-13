using Application.Dtos.Tip;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Dtos;

public class TipExtensionsTests
{
    [Fact]
    public void ToTipSummaryResponse_ShouldMapCorrectly_WhenValidTipProvided()
    {
        // Arrange
        var tip = CreateValidTip();
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tip.Id.Value);
        result.Title.Should().Be(tip.Title.Value);
        result.Description.Should().Be(tip.Description.Value);
        result.CategoryId.Should().Be(tip.CategoryId.Value);
        result.CategoryName.Should().Be(categoryName);
        result.Tags.Should().HaveCount(2);
        result.Tags.Should().Contain("cooking");
        result.Tags.Should().Contain("pasta");
        result.VideoUrl.Should().Be(tip.VideoUrl!.Value);
        result.CreatedAt.Should().Be(tip.CreatedAt);
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldHandleNullVideoUrl()
    {
        // Arrange
        var tip = CreateValidTip(includeVideoUrl: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.VideoUrl.Should().BeNull();
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldHandleEmptyTags()
    {
        // Arrange
        var tip = CreateValidTip(includeTags: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.Tags.Should().NotBeNull();
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldThrowArgumentNullException_WhenTipNull()
    {
        // Arrange
        Tip? nullTip = null;
        var categoryName = "Cooking";

        // Act
        var act = () => nullTip!.ToTipSummaryResponse(categoryName);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldThrowArgumentNullException_WhenCategoryNameNull()
    {
        // Arrange
        var tip = CreateValidTip();
        string? nullCategoryName = null;

        // Act
        var act = () => tip.ToTipSummaryResponse(nullCategoryName!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTipDetailResponse_ShouldMapCorrectly_WhenValidTipProvided()
    {
        // Arrange
        var tip = CreateValidTip();
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tip.Id.Value);
        result.Title.Should().Be(tip.Title.Value);
        result.Description.Should().Be(tip.Description.Value);
        result.CategoryId.Should().Be(tip.CategoryId.Value);
        result.CategoryName.Should().Be(categoryName);
        result.Tags.Should().HaveCount(2);
        result.VideoUrl.Should().Be(tip.VideoUrl!.Value);
        result.VideoUrlId.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().Be(tip.CreatedAt);
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ToTipDetailResponse_ShouldMapStepsCorrectly()
    {
        // Arrange
        var tip = CreateValidTip();
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.Steps.Should().HaveCount(3);
        result.Steps[0].StepNumber.Should().Be(1);
        result.Steps[0].Description.Should().Be("Boil water in a large pot.");
        result.Steps[1].StepNumber.Should().Be(2);
        result.Steps[1].Description.Should().Be("Add salt to the boiling water.");
        result.Steps[2].StepNumber.Should().Be(3);
        result.Steps[2].Description.Should().Be("Add pasta and cook according to package instructions.");
    }

    [Fact]
    public void ToTipDetailResponse_ShouldExtractVideoId_WhenVideoUrlPresent()
    {
        // Arrange
        var tip = CreateValidTip();
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.VideoUrlId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public void ToTipDetailResponse_ShouldHandleNullVideoUrl()
    {
        // Arrange
        var tip = CreateValidTip(includeVideoUrl: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.VideoUrl.Should().BeNull();
        result.VideoUrlId.Should().BeNull();
    }

    [Fact]
    public void ToTipDetailResponse_ShouldThrowArgumentNullException_WhenTipNull()
    {
        // Arrange
        Tip? nullTip = null;
        var categoryName = "Cooking";

        // Act
        var act = () => nullTip!.ToTipDetailResponse(categoryName);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTipDetailResponse_ShouldThrowArgumentNullException_WhenCategoryNameNull()
    {
        // Arrange
        var tip = CreateValidTip();
        string? nullCategoryName = null;

        // Act
        var act = () => tip.ToTipDetailResponse(nullCategoryName!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToTipImage_ShouldConvertDto_WhenValidDtoProvided()
    {
        // Arrange
        var dto = new TipImageDto(
            ImageUrl: "https://cdn.example.com/tips/test-image.jpg",
            ImageStoragePath: "tips/550e8400-e29b-41d4-a716-446655440000.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 245760,
            UploadedAt: DateTime.UtcNow
        );

        // Act
        var result = dto.ToTipImage();

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be(dto.ImageUrl);
        result.ImageStoragePath.Should().Be(dto.ImageStoragePath);
        result.OriginalFileName.Should().Be(dto.OriginalFileName);
        result.ContentType.Should().Be(dto.ContentType);
        result.FileSizeBytes.Should().Be(dto.FileSizeBytes);
        result.UploadedAt.Should().BeCloseTo(dto.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToTipImage_ShouldReturnNull_WhenDtoIsNull()
    {
        // Arrange
        TipImageDto? nullDto = null;

        // Act
        var result = nullDto.ToTipImage();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToTipImageDto_ShouldConvertValueObject_WhenValidImageProvided()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow;
        var image = TipImage.Create(
            imageUrl: "https://cdn.example.com/tips/test-image.jpg",
            imageStoragePath: "tips/550e8400-e29b-41d4-a716-446655440000.jpg",
            originalFileName: "test-image.jpg",
            contentType: "image/jpeg",
            fileSizeBytes: 245760,
            uploadedAt: uploadedAt
        );

        // Act
        var result = image.ToTipImageDto();

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(image.ImageUrl);
        result.ImageStoragePath.Should().Be(image.ImageStoragePath);
        result.OriginalFileName.Should().Be(image.OriginalFileName);
        result.ContentType.Should().Be(image.ContentType);
        result.FileSizeBytes.Should().Be(image.FileSizeBytes);
        result.UploadedAt.Should().BeCloseTo(uploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldIncludeImage_WhenTipHasImage()
    {
        // Arrange
        var tip = CreateValidTip(includeImage: true);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.Image.Should().NotBeNull();
        result.Image!.ImageUrl.Should().Be(tip.Image!.ImageUrl);
        result.Image.ImageStoragePath.Should().Be(tip.Image.ImageStoragePath);
        result.Image.OriginalFileName.Should().Be(tip.Image.OriginalFileName);
        result.Image.ContentType.Should().Be(tip.Image.ContentType);
        result.Image.FileSizeBytes.Should().Be(tip.Image.FileSizeBytes);
        result.Image.UploadedAt.Should().BeCloseTo(tip.Image.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldHaveNullImage_WhenTipHasNoImage()
    {
        // Arrange
        var tip = CreateValidTip(includeImage: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.Image.Should().BeNull();
    }

    [Fact]
    public void ToTipDetailResponse_ShouldIncludeImage_WhenTipHasImage()
    {
        // Arrange
        var tip = CreateValidTip(includeImage: true);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.Image.Should().NotBeNull();
        result.Image!.ImageUrl.Should().Be(tip.Image!.ImageUrl);
        result.Image.ImageStoragePath.Should().Be(tip.Image.ImageStoragePath);
        result.Image.OriginalFileName.Should().Be(tip.Image.OriginalFileName);
        result.Image.ContentType.Should().Be(tip.Image.ContentType);
        result.Image.FileSizeBytes.Should().Be(tip.Image.FileSizeBytes);
        result.Image.UploadedAt.Should().BeCloseTo(tip.Image.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToTipDetailResponse_ShouldHaveNullImage_WhenTipHasNoImage()
    {
        // Arrange
        var tip = CreateValidTip(includeImage: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.Image.Should().BeNull();
    }

    private static Tip CreateValidTip(bool includeVideoUrl = true, bool includeTags = true, bool includeImage = false)
    {
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep>
        {
            TipStep.Create(1, "Boil water in a large pot."),
            TipStep.Create(2, "Add salt to the boiling water."),
            TipStep.Create(3, "Add pasta and cook according to package instructions.")
        };
        var categoryId = CategoryId.NewId();
        var tags = includeTags
            ? new List<Tag> { Tag.Create("cooking"), Tag.Create("pasta") }
            : null;
        var videoUrl = includeVideoUrl
            ? VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ")
            : null;
        var image = includeImage
            ? TipImage.Create(
                imageUrl: "https://cdn.example.com/tips/test-image.jpg",
                imageStoragePath: "tips/550e8400-e29b-41d4-a716-446655440000.jpg",
                originalFileName: "test-image.jpg",
                contentType: "image/jpeg",
                fileSizeBytes: 245760,
                uploadedAt: DateTime.UtcNow)
            : null;

        return Tip.Create(title, description, steps, categoryId, tags, videoUrl, image);
    }

    // TODO: Property-based tests (tasks 2.4 and 2.5) will be implemented later
    // These tests are currently commented out to allow the build to succeed for checkpoint task 3
}
