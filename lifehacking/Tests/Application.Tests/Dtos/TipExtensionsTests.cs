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
        result.YouTubeUrl.Should().Be(tip.YouTubeUrl!.Value);
        result.CreatedAt.Should().Be(tip.CreatedAt);
    }

    [Fact]
    public void ToTipSummaryResponse_ShouldHandleNullYouTubeUrl()
    {
        // Arrange
        var tip = CreateValidTip(includeYouTubeUrl: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipSummaryResponse(categoryName);

        // Assert
        result.YouTubeUrl.Should().BeNull();
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
        result.YouTubeUrl.Should().Be(tip.YouTubeUrl!.Value);
        result.YouTubeVideoId.Should().NotBeNullOrEmpty();
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
    public void ToTipDetailResponse_ShouldExtractVideoId_WhenYouTubeUrlPresent()
    {
        // Arrange
        var tip = CreateValidTip();
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.YouTubeVideoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public void ToTipDetailResponse_ShouldHandleNullYouTubeUrl()
    {
        // Arrange
        var tip = CreateValidTip(includeYouTubeUrl: false);
        var categoryName = "Cooking";

        // Act
        var result = tip.ToTipDetailResponse(categoryName);

        // Assert
        result.YouTubeUrl.Should().BeNull();
        result.YouTubeVideoId.Should().BeNull();
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

    private static Tip CreateValidTip(bool includeYouTubeUrl = true, bool includeTags = true)
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
        var youtubeUrl = includeYouTubeUrl
            ? YouTubeUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ")
            : null;

        return Tip.Create(title, description, steps, categoryId, tags, youtubeUrl);
    }
}
