using Domain.Entities;
using Domain.ValueObject;

namespace Infrastructure.Tests;

/// <summary>
/// Provides factory methods for creating realistic test data entities.
/// This class centralizes test data creation to ensure consistency and realism across all test projects.
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a user with realistic data for testing.
    /// </summary>
    /// <param name="email">Optional email address. If not provided, generates a unique email using a GUID.</param>
    /// <param name="name">Optional user name. If not provided, generates a unique name using a GUID.</param>
    /// <param name="externalAuthId">Optional external authentication identifier. If not provided, generates a unique ID using a GUID.</param>
    /// <returns>A new User entity with the specified or generated values.</returns>
    public static User CreateUser(
        string? email = null,
        string? name = null,
        string? externalAuthId = null)
    {
        var userEmail = Email.Create(email ?? $"user{Guid.NewGuid():N}@example.com");
        var userName = UserName.Create(name ?? $"Test User {Guid.NewGuid():N}");
        var authId = ExternalAuthIdentifier.Create(externalAuthId ?? $"auth_{Guid.NewGuid():N}");

        return User.Create(userEmail, userName, authId);
    }

    /// <summary>
    /// Creates a category with realistic data for testing.
    /// </summary>
    /// <param name="name">Optional category name. If not provided, generates a unique name using a GUID.</param>
    /// <returns>A new Category entity with the specified or generated name.</returns>
    public static Category CreateCategory(string? name = null)
    {
        var categoryName = name ?? $"Test Category {Guid.NewGuid():N}";
        return Category.Create(categoryName);
    }

    /// <summary>
    /// Creates a tip with realistic data and relationships.
    /// Establishes a relationship with the specified category.
    /// </summary>
    /// <param name="categoryId">The ID of the category this tip belongs to.</param>
    /// <param name="title">Optional tip title. If not provided, generates a unique title using a GUID.</param>
    /// <param name="description">Optional tip description. If not provided, generates a realistic description using a GUID.</param>
    /// <param name="tags">Optional collection of tag strings. If not provided, uses default tags ["test", "sample"].</param>
    /// <returns>A new Tip entity with the specified or generated values and multiple steps.</returns>
    public static Tip CreateTip(
        CategoryId categoryId,
        string? title = null,
        string? description = null,
        IEnumerable<string>? tags = null)
    {
        var tipTitle = TipTitle.Create(title ?? $"Test Tip {Guid.NewGuid():N}");
        var tipDescription = TipDescription.Create(
            description ?? $"This is a test tip description with enough content to be realistic. {Guid.NewGuid():N}");

        var steps = new[]
        {
            TipStep.Create(1, "First step of the tip - follow this instruction carefully"),
            TipStep.Create(2, "Second step of the tip - complete this action next")
        };

        var tipTags = (tags ?? new[] { "test", "sample" }).Select(Tag.Create).ToList();

        return Tip.Create(tipTitle, tipDescription, steps, categoryId, tipTags);
    }

    /// <summary>
    /// Creates multiple tips with varied content for search testing.
    /// Each tip has different search terms in its title, description, and tags to exercise different query paths.
    /// </summary>
    /// <param name="categoryId">The ID of the category these tips belong to.</param>
    /// <param name="count">The number of tips to create. Default is 5.</param>
    /// <returns>A read-only list of Tip entities with varied content suitable for search testing.</returns>
    public static IReadOnlyList<Tip> CreateTipsForSearch(
        CategoryId categoryId,
        int count = 5)
    {
        var tips = new List<Tip>();
        var searchTerms = new[] { "productivity", "health", "finance", "learning", "organization" };

        for (int i = 0; i < count; i++)
        {
            var searchTerm = searchTerms[i % searchTerms.Length];
            var tip = CreateTip(
                categoryId,
                title: $"Tip about {searchTerm} #{i}",
                description: $"This tip helps with {searchTerm} and provides valuable insights for improvement.",
                tags: new[] { searchTerm, "test" });

            tips.Add(tip);
        }

        return tips;
    }
}
