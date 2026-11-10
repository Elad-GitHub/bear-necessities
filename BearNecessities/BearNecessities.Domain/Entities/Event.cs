using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class Event : AuditableEntity<EventId>
{
    private readonly List<DateTime> _exDates = new();
    private readonly List<Category> _categories = new();
    private readonly List<EventOccurrence> _occurrences = new();
    private readonly List<Media> _media = new();

    private Event() { }

    public PlaceId? PlaceId { get; private set; }
    public UserId OwnerId { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public EventType EventType { get; private set; }
    public DateTime? StartAt { get; private set; }
    public DateTime? EndAt { get; private set; }
    public string Timezone { get; private set; } = default!;
    public string? RRule { get; private set; }
    public decimal? Price { get; private set; }
    public string? Currency { get; private set; }
    public bool IsFree { get; private set; }
    public bool IsOnline { get; private set; }
    public string? OnlineUrl { get; private set; }
    public int? MaxCapacity { get; private set; }
    public ListingStatus Status { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public IReadOnlyCollection<DateTime> ExDates => _exDates.AsReadOnly();
    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();
    public IReadOnlyCollection<EventOccurrence> Occurrences => _occurrences.AsReadOnly();
    public IReadOnlyCollection<Media> Media => _media.AsReadOnly();

    public static Event Create(string title, EventType type, string timezone, UserId ownerId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        return new Event
        {
            Id = new EventId(Guid.NewGuid()),
            Title = title.Trim(),
            EventType = type,
            Timezone = timezone,
            OwnerId = ownerId,
            Status = ListingStatus.Draft,
            IsFree = true
        };
    }

    public void Approve(UserId reviewerId)
    {
        if (Status != ListingStatus.PendingReview)
            throw new InvalidOperationException("Only listings in PendingReview status can be approved.");

        Status = ListingStatus.Live;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ListingStatus.Cancelled)
            return;
        if (Status == ListingStatus.Archived)
            throw new InvalidOperationException("Archived events cannot be cancelled.");

        Status = ListingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Submit()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException("Only Draft events can be submitted for review.");

        Status = ListingStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestReview()
    {
        Status = ListingStatus.PendingReview;
        ReviewedBy = null;
        ReviewedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(UserId reviewerId)
    {
        if (Status != ListingStatus.PendingReview)
            throw new InvalidOperationException("Only listings in PendingReview status can be rejected.");

        Status = ListingStatus.Suspended;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCategory(Category category)
    {
        if (_categories.Count >= 5)
            throw new InvalidOperationException("An event cannot have more than 5 categories.");
        if (_categories.Any(c => c.Id == category.Id))
            return;

        _categories.Add(category);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCategories()
    {
        _categories.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTitle(string title)
    {
        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlaceId(PlaceId? placeId)
    {
        PlaceId = placeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStartAt(DateTime? startAt)
    {
        StartAt = startAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEndAt(DateTime? endAt)
    {
        EndAt = endAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRRule(string? rrule)
    {
        RRule = rrule;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrice(decimal? price, string? currency, bool isFree)
    {
        Price = price;
        Currency = currency;
        IsFree = isFree;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetIsOnline(bool isOnline, string? onlineUrl)
    {
        IsOnline = isOnline;
        OnlineUrl = onlineUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaxCapacity(int? maxCapacity)
    {
        MaxCapacity = maxCapacity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTimezone(string timezone)
    {
        Timezone = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEventType(EventType eventType)
    {
        EventType = eventType;
        UpdatedAt = DateTime.UtcNow;
    }
}
