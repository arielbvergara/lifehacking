using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck.Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Property-based tests for TestDataFactory.
/// Feature: firestore-test-infrastructure-improvements
/// Property 10: Test Data Factory Produces Valid Entities
/// Validates: Requirements 6.2, 6.3, 6.4
/// </summary>
public class TestDataFactoryPropertyTests
{
    // Feature: firestore-test-infrastructure-improvements, Property 10: Test Data Factory Produces Valid Entities
    // For any entity created by TestDataFactory methods, the entity should have valid properties
    // (non-null required fields, realistic values), and tips created by the factory should have
    // valid CategoryId references to existing categories.
    // Validates: Requirements 6.2, 6.3, 6.4

    /// <summary>
    /// Property: CreateUser should produce valid users with all required fields populated.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateUser_ShouldProduceValidUser_WhenCalledWithoutParameters()
    {
        // Act
        var user = TestDataFactory.CreateUser();

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeNull();
        user.Email.Should().NotBeNull();
        user.Email.Value.Should().NotBeNullOrWhiteSpace();
        user.Email.Value.Should().Contain("@");
        user.Name.Should().NotBeNull();
        user.Name.Value.Should().NotBeNullOrWhiteSpace();
        user.ExternalAuthId.Should().NotBeNull();
        user.ExternalAuthId.Value.Should().NotBeNullOrWhiteSpace();
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Property: CreateUser should accept custom parameters and produce valid users.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateUser_ShouldProduceValidUser_WhenCalledWithCustomEmail()
    {
        // Arrange
        var customEmail = $"test{Guid.NewGuid():N}@example.com";

        // Act
        var user = TestDataFactory.CreateUser(email: customEmail);

        // Assert
        user.Should().NotBeNull();
        user.Email.Value.Should().Be(customEmail.ToLowerInvariant());
    }

    /// <summary>
    /// Property: CreateCategory should produce valid categories with all required fields populated.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateCategory_ShouldProduceValidCategory_WhenCalledWithoutParameters()
    {
        // Act
        var category = TestDataFactory.CreateCategory();

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBeNull();
        category.Name.Should().NotBeNullOrWhiteSpace();
        category.Name.Length.Should().BeGreaterOrEqualTo(Category.MinNameLength);
        category.Name.Length.Should().BeLessOrEqualTo(Category.MaxNameLength);
        category.IsDeleted.Should().BeFalse();
        category.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Property: CreateCategory should accept custom name and produce valid category.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateCategory_ShouldProduceValidCategory_WhenCalledWithCustomName()
    {
        // Arrange
        var customName = "Custom Category Name";

        // Act
        var category = TestDataFactory.CreateCategory(customName);

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be(customName);
    }

    /// <summary>
    /// Property: CreateTip should produce valid tips with all required fields populated.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTip_ShouldProduceValidTip_WhenCalledWithCategoryId()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();

        // Act
        var tip = TestDataFactory.CreateTip(category.Id);

        // Assert
        tip.Should().NotBeNull();
        tip.Id.Should().NotBeNull();
        tip.Title.Should().NotBeNull();
        tip.Title.Value.Should().NotBeNullOrWhiteSpace();
        tip.Title.Value.Length.Should().BeGreaterOrEqualTo(TipTitle.MinLength);
        tip.Title.Value.Length.Should().BeLessOrEqualTo(TipTitle.MaxLength);
        tip.Description.Should().NotBeNull();
        tip.Description.Value.Should().NotBeNullOrWhiteSpace();
        tip.Description.Value.Length.Should().BeGreaterOrEqualTo(TipDescription.MinLength);
        tip.Description.Value.Length.Should().BeLessOrEqualTo(TipDescription.MaxLength);
        tip.CategoryId.Should().Be(category.Id);
        tip.Steps.Should().NotBeEmpty();
        tip.Steps.Should().HaveCountGreaterOrEqualTo(1);
        tip.Tags.Should().NotBeEmpty();
        tip.IsDeleted.Should().BeFalse();
        tip.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Property: CreateTip should establish valid category relationship.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTip_ShouldEstablishValidCategoryRelationship_WhenCalledWithCategoryId()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();

        // Act
        var tip = TestDataFactory.CreateTip(category.Id);

        // Assert - Tip should reference the correct category
        tip.CategoryId.Should().Be(category.Id);
        tip.CategoryId.Value.Should().Be(category.Id.Value);
    }

    /// <summary>
    /// Property: CreateTip should accept custom parameters and produce valid tip.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTip_ShouldProduceValidTip_WhenCalledWithCustomParameters()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        var customTitle = "Custom Tip Title";
        var customDescription = "This is a custom tip description with enough content to be valid.";
        var customTags = new[] { "custom", "test" };

        // Act
        var tip = TestDataFactory.CreateTip(category.Id, customTitle, customDescription, customTags);

        // Assert
        tip.Should().NotBeNull();
        tip.Title.Value.Should().Be(customTitle);
        tip.Description.Value.Should().Be(customDescription);
        tip.Tags.Should().HaveCount(2);
        tip.Tags.Select(t => t.Value).Should().Contain(customTags);
    }

    /// <summary>
    /// Property: CreateTipsForSearch should produce the requested number of valid tips.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTipsForSearch_ShouldProduceRequestedNumberOfTips_WhenCalledWithCount()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        var count = 7;

        // Act
        var tips = TestDataFactory.CreateTipsForSearch(category.Id, count);

        // Assert
        tips.Should().NotBeNull();
        tips.Should().HaveCount(count);
        tips.Should().OnlyContain(tip => tip.CategoryId == category.Id);
        tips.Should().OnlyContain(tip => !tip.IsDeleted);
        tips.Should().OnlyContain(tip => tip.Steps.Count >= 1);
        tips.Should().OnlyContain(tip => tip.Tags.Count >= 1);
    }

    /// <summary>
    /// Property: CreateTipsForSearch should produce varied content for search testing.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTipsForSearch_ShouldProduceVariedContent_WhenCalledWithMultipleTips()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();

        // Act
        var tips = TestDataFactory.CreateTipsForSearch(category.Id);

        // Assert - Tips should have varied content
        var uniqueTitles = tips.Select(t => t.Title.Value).Distinct().Count();
        var uniqueDescriptions = tips.Select(t => t.Description.Value).Distinct().Count();

        uniqueTitles.Should().Be(5, "each tip should have a unique title");
        uniqueDescriptions.Should().Be(5, "each tip should have a unique description");

        // At least some tips should have different tags
        var allTags = tips.SelectMany(t => t.Tags.Select(tag => tag.Value)).Distinct().ToList();
        allTags.Should().HaveCountGreaterThan(2, "tips should have varied tags for search testing");
    }

    /// <summary>
    /// Property: CreateTipsForSearch should use default count when not specified.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateTipsForSearch_ShouldUseDefaultCount_WhenCountNotSpecified()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();

        // Act
        var tips = TestDataFactory.CreateTipsForSearch(category.Id);

        // Assert
        tips.Should().HaveCount(5, "default count should be 5");
    }

    /// <summary>
    /// Property: Multiple calls to CreateUser should produce unique users.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateUser_ShouldProduceUniqueUsers_WhenCalledMultipleTimes()
    {
        // Act
        var users = Enumerable.Range(0, 10)
            .Select(_ => TestDataFactory.CreateUser())
            .ToList();

        // Assert
        var uniqueEmails = users.Select(u => u.Email.Value).Distinct().Count();
        var uniqueIds = users.Select(u => u.Id.Value).Distinct().Count();

        uniqueEmails.Should().Be(10, "each user should have a unique email");
        uniqueIds.Should().Be(10, "each user should have a unique ID");
    }

    /// <summary>
    /// Property: Multiple calls to CreateCategory should produce unique categories.
    /// </summary>
    [Property(MaxTest = 100)]
    public void CreateCategory_ShouldProduceUniqueCategories_WhenCalledMultipleTimes()
    {
        // Act
        var categories = Enumerable.Range(0, 10)
            .Select(_ => TestDataFactory.CreateCategory())
            .ToList();

        // Assert
        var uniqueNames = categories.Select(c => c.Name).Distinct().Count();
        var uniqueIds = categories.Select(c => c.Id.Value).Distinct().Count();

        uniqueNames.Should().Be(10, "each category should have a unique name");
        uniqueIds.Should().Be(10, "each category should have a unique ID");
    }
}
