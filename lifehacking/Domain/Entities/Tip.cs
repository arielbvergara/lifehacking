using Domain.ValueObject;

namespace Domain.Entities;

/// <summary>
/// Represents a tip with title, description, steps, and optional metadata.
/// </summary>
public sealed class Tip
{
    /// <summary>
    /// The maximum number of tags allowed per tip.
    /// </summary>
    private const int MaxTagsCount = 10;

    private readonly List<TipStep> _steps;
    private readonly List<Tag> _tags;

    public TipId Id { get; private set; }
    public TipTitle Title { get; private set; }
    public TipDescription Description { get; private set; }
    public IReadOnlyList<TipStep> Steps => _steps.AsReadOnly();
    public CategoryId CategoryId { get; private set; }
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();
    public VideoUrl? VideoUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Tip(
        TipId id,
        TipTitle title,
        TipDescription description,
        IEnumerable<TipStep> steps,
        CategoryId categoryId,
        IEnumerable<Tag> tags,
        VideoUrl? videoUrl,
        DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        _steps = steps.ToList();
        CategoryId = categoryId;
        _tags = tags.ToList();
        VideoUrl = videoUrl;
        CreatedAt = createdAt;
        IsDeleted = false;
        DeletedAt = null;
    }

    public static Tip Create(
        TipTitle title,
        TipDescription description,
        IEnumerable<TipStep> steps,
        CategoryId categoryId,
        IEnumerable<Tag>? tags = null,
        VideoUrl? videoUrl = null)
    {
        var stepsList = steps.ToList();
        ValidateSteps(stepsList);

        var tagsList = tags?.ToList() ?? new List<Tag>();
        ValidateTags(tagsList);

        var tip = new Tip(
            TipId.NewId(),
            title,
            description,
            stepsList,
            categoryId,
            tagsList,
            videoUrl,
            DateTime.UtcNow);

        return tip;
    }

    public void UpdateTitle(TipTitle title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(TipDescription description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSteps(IEnumerable<TipStep> steps)
    {
        var stepsList = steps.ToList();
        ValidateSteps(stepsList);

        _steps.Clear();
        _steps.AddRange(stepsList);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCategory(CategoryId categoryId)
    {
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTags(IEnumerable<Tag> tags)
    {
        var tagsList = tags.ToList();
        ValidateTags(tagsList);

        _tags.Clear();
        _tags.AddRange(tagsList);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateVideoUrl(VideoUrl? videoUrl)
    {
        VideoUrl = videoUrl;
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
    /// Factory method used by persistence layers to rehydrate a <see cref="Tip"/>
    /// from stored values without coupling domain logic to any specific database technology.
    /// </summary>
    public static Tip FromPersistence(
        TipId id,
        TipTitle title,
        TipDescription description,
        IEnumerable<TipStep> steps,
        CategoryId categoryId,
        IEnumerable<Tag> tags,
        VideoUrl? videoUrl,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isDeleted,
        DateTime? deletedAt)
    {
        var tip = new Tip(
            id,
            title,
            description,
            steps,
            categoryId,
            tags,
            videoUrl,
            createdAt)
        {
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

        return tip;
    }

    private static void ValidateSteps(List<TipStep> steps)
    {
        if (steps.Count == 0)
        {
            throw new ArgumentException("Tip must have at least one step", nameof(steps));
        }
    }

    private static void ValidateTags(List<Tag> tags)
    {
        if (tags.Count > MaxTagsCount)
        {
            throw new ArgumentException($"Tip cannot have more than {MaxTagsCount} tags", nameof(tags));
        }
    }
}
