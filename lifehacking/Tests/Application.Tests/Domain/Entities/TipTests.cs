using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.Entities;

public class TipTests
{
    [Fact]
    public void Create_ShouldCreateTipWithExpectedValues_WhenValidParametersProvided()
    {
        // Arrange
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep>
        {
            TipStep.Create(1, "Boil water in a large pot."),
            TipStep.Create(2, "Add salt to the boiling water."),
            TipStep.Create(3, "Add pasta and cook according to package instructions.")
        };
        var categoryId = CategoryId.NewId();
        var tags = new List<Tag> { Tag.Create("cooking"), Tag.Create("pasta") };
        var videoUrl = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        var before = DateTime.UtcNow;

        // Act
        var tip = Tip.Create(title, description, steps, categoryId, tags, videoUrl);
        var after = DateTime.UtcNow;

        // Assert
        tip.Should().NotBeNull();
        tip.Title.Should().Be(title);
        tip.Description.Should().Be(description);
        tip.Steps.Should().HaveCount(3);
        tip.Steps.Should().BeEquivalentTo(steps);
        tip.CategoryId.Should().Be(categoryId);
        tip.Tags.Should().HaveCount(2);
        tip.Tags.Should().BeEquivalentTo(tags);
        tip.YouTubeUrl.Should().Be(videoUrl);

        tip.Id.Should().NotBe(null);
        tip.Id.Value.Should().NotBe(Guid.Empty);

        tip.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        tip.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenStepsCollectionEmpty()
    {
        // Arrange
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var emptySteps = new List<TipStep>();
        var categoryId = CategoryId.NewId();

        // Act
        var act = () => Tip.Create(title, description, emptySteps, categoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip must have at least one step*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenTooManyTags()
    {
        // Arrange
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep> { TipStep.Create(1, "Boil water in a large pot.") };
        var categoryId = CategoryId.NewId();
        var tooManyTags = Enumerable.Range(1, 11).Select(i => Tag.Create($"tag{i}")).ToList();

        // Act
        var act = () => Tip.Create(title, description, steps, categoryId, tooManyTags);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip cannot have more than 10 tags*");
    }

    [Fact]
    public void Create_ShouldDefaultToEmptyTagsCollection_WhenTagsNull()
    {
        // Arrange
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep> { TipStep.Create(1, "Boil water in a large pot.") };
        var categoryId = CategoryId.NewId();

        // Act
        var tip = Tip.Create(title, description, steps, categoryId, tags: null);

        // Assert
        tip.Tags.Should().NotBeNull();
        tip.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldAllowNullYouTubeUrl()
    {
        // Arrange
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep> { TipStep.Create(1, "Boil water in a large pot.") };
        var categoryId = CategoryId.NewId();

        // Act
        var tip = Tip.Create(title, description, steps, categoryId, youtubeUrl: null);

        // Assert
        tip.YouTubeUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateTitle_ShouldUpdateTitleAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newTitle = TipTitle.Create("How to cook perfect pasta");
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateTitle(newTitle);
        var after = DateTime.UtcNow;

        // Assert
        tip.Title.Should().Be(newTitle);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateDescriptionAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newDescription = TipDescription.Create("An updated comprehensive guide to cooking perfect pasta every time.");
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateDescription(newDescription);
        var after = DateTime.UtcNow;

        // Assert
        tip.Description.Should().Be(newDescription);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateSteps_ShouldUpdateStepsAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newSteps = new List<TipStep>
        {
            TipStep.Create(1, "Fill a large pot with water."),
            TipStep.Create(2, "Bring water to a rolling boil.")
        };
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateSteps(newSteps);
        var after = DateTime.UtcNow;

        // Assert
        tip.Steps.Should().HaveCount(2);
        tip.Steps.Should().BeEquivalentTo(newSteps);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateSteps_ShouldThrowArgumentException_WhenEmptyCollection()
    {
        // Arrange
        var tip = CreateValidTip();
        var emptySteps = new List<TipStep>();

        // Act
        var act = () => tip.UpdateSteps(emptySteps);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip must have at least one step*");
    }

    [Fact]
    public void UpdateCategory_ShouldUpdateCategoryAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newCategoryId = CategoryId.NewId();
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateCategory(newCategoryId);
        var after = DateTime.UtcNow;

        // Assert
        tip.CategoryId.Should().Be(newCategoryId);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateTags_ShouldUpdateTagsAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newTags = new List<Tag> { Tag.Create("italian"), Tag.Create("quick-meal") };
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateTags(newTags);
        var after = DateTime.UtcNow;

        // Assert
        tip.Tags.Should().HaveCount(2);
        tip.Tags.Should().BeEquivalentTo(newTags);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateTags_ShouldThrowArgumentException_WhenTooManyTags()
    {
        // Arrange
        var tip = CreateValidTip();
        var tooManyTags = Enumerable.Range(1, 11).Select(i => Tag.Create($"tag{i}")).ToList();

        // Act
        var act = () => tip.UpdateTags(tooManyTags);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tip cannot have more than 10 tags*");
    }

    [Fact]
    public void UpdateYouTubeUrl_ShouldUpdateUrlAndSetUpdatedAt()
    {
        // Arrange
        var tip = CreateValidTip();
        var originalCreatedAt = tip.CreatedAt;
        var newUrl = VideoUrl.Create("https://www.youtube.com/watch?v=newVideoId");
        var before = DateTime.UtcNow;

        // Act
        tip.UpdateYouTubeUrl(newUrl);
        var after = DateTime.UtcNow;

        // Assert
        tip.YouTubeUrl.Should().Be(newUrl);
        tip.CreatedAt.Should().Be(originalCreatedAt);
        tip.UpdatedAt.Should().NotBeNull();
        tip.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void FromPersistence_ShouldRehydrateTipCorrectly()
    {
        // Arrange
        var id = TipId.NewId();
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep> { TipStep.Create(1, "Boil water in a large pot.") };
        var categoryId = CategoryId.NewId();
        var tags = new List<Tag> { Tag.Create("cooking") };
        var videoUrl = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var tip = Tip.FromPersistence(id, title, description, steps, categoryId, tags, videoUrl, createdAt, updatedAt, false, null);

        // Assert
        tip.Should().NotBeNull();
        tip.Id.Should().Be(id);
        tip.Title.Should().Be(title);
        tip.Description.Should().Be(description);
        tip.Steps.Should().BeEquivalentTo(steps);
        tip.CategoryId.Should().Be(categoryId);
        tip.Tags.Should().BeEquivalentTo(tags);
        tip.YouTubeUrl.Should().Be(videoUrl);
        tip.CreatedAt.Should().Be(createdAt);
        tip.UpdatedAt.Should().Be(updatedAt);
    }

    private static Tip CreateValidTip()
    {
        var title = TipTitle.Create("How to cook pasta");
        var description = TipDescription.Create("A comprehensive guide to cooking perfect pasta every time.");
        var steps = new List<TipStep> { TipStep.Create(1, "Boil water in a large pot.") };
        var categoryId = CategoryId.NewId();

        return Tip.Create(title, description, steps, categoryId);
    }
}
