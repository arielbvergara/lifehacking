using Domain.ValueObject;

namespace Domain.Entities;

/// <summary>
/// Represents a category for organizing tips.
/// </summary>
public sealed class Category
{
    /// <summary>
    /// The minimum allowed length for a category name.
    /// </summary>
    public const int MinNameLength = 2;

    /// <summary>
    /// The maximum allowed length for a category name.
    /// </summary>
    public const int MaxNameLength = 100;

    public CategoryId Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Category(
        CategoryId id,
        string name,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        IsDeleted = false;
        DeletedAt = null;
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

    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return; // Idempotent - already deleted
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method used by persistence layers to rehydrate a <see cref="Category"/>
    /// from stored values without coupling domain logic to any specific database technology.
    /// </summary>
    public static Category FromPersistence(
        CategoryId id,
        string name,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isDeleted,
        DateTime? deletedAt)
    {
        var category = new Category(
            id,
            name,
            createdAt);

        category.UpdatedAt = updatedAt;
        category.IsDeleted = isDeleted;
        category.DeletedAt = deletedAt;

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
