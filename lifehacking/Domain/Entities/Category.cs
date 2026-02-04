using Domain.ValueObject;

namespace Domain.Entities;

public sealed class Category
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 100;

    public CategoryId Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Parameterless constructor for EF Core
    private Category()
    {
    }

    private Category(
        CategoryId id,
        string name,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public static Category Create(string name)
    {
        ValidateName(name);

        var category = new Category(
            CategoryId.NewId(),
            name.Trim(),
            DateTime.UtcNow);

        return category;
    }

    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method used by persistence layers to rehydrate a <see cref="Category"/>
    /// from stored values without coupling domain logic to any specific database technology.
    /// </summary>
    public static Category FromPersistence(
        CategoryId id,
        string name,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var category = new Category(
            id,
            name,
            createdAt);

        category.UpdatedAt = updatedAt;

        return category;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length < MinNameLength)
        {
            throw new ArgumentException($"Category name must be at least {MinNameLength} characters", nameof(name));
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Category name cannot exceed {MaxNameLength} characters", nameof(name));
        }
    }
}
