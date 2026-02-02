using Domain.ValueObject;

namespace Domain.Entities;

public sealed class Tip
{
    private const int MaxTagsCount = 10;

    private readonly List<TipStep> _steps;
    private readonly List<Tag> _tags;

    public TipId Id { get; }
    public TipTitle Title { get; private set; }
    public TipDescription Description { get; private set; }
    public IReadOnlyList<TipStep> Steps => _steps.AsReadOnly();
    public CategoryId CategoryId { get; private set; }
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();
    public YouTubeUrl? YouTubeUrl { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    private Tip(
        TipId id,
        TipTitle title,
        TipDescription description,
        IEnumerable<TipStep> steps,
        CategoryId categoryId,
        IEnumerable<Tag> tags,
        YouTubeUrl? youtubeUrl,
        DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        _steps = steps.ToList();
        CategoryId = categoryId;
        _tags = tags.ToList();
        YouTubeUrl = youtubeUrl;
        CreatedAt = createdAt;
    }

    public static Tip Create(
        TipTitle title,
        TipDescription description,
        IEnumerable<TipStep> steps,
        CategoryId categoryId,
        IEnumerable<Tag>? tags = null,
        YouTubeUrl? youtubeUrl = null)
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
            youtubeUrl,
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

    public void UpdateYouTubeUrl(YouTubeUrl? youtubeUrl)
    {
        YouTubeUrl = youtubeUrl;
        UpdatedAt = DateTime.UtcNow;
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
        YouTubeUrl? youtubeUrl,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var tip = new Tip(
            id,
            title,
            description,
            steps,
            categoryId,
            tags,
            youtubeUrl,
            createdAt)
        {
            UpdatedAt = updatedAt
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
