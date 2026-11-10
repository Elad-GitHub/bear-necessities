using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class Place : AuditableEntity<PlaceId>
{
    private readonly List<Category> _categories = new();
    private readonly List<PlaceExternalLink> _externalLinks = new();
    private readonly List<Media> _media = new();

    private Place() { }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string H3IndexRes8 { get; private set; } = default!;
    public string H3IndexRes5 { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string Timezone { get; private set; } = default!;
    public ListingSourceType Source { get; private set; }
    public string? ExternalSourceId { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsPermanent { get; private set; }
    public ListingStatus Status { get; private set; }
    public UserId OwnerId { get; private set; } = default!;
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();
    public IReadOnlyCollection<PlaceExternalLink> ExternalLinks => _externalLinks.AsReadOnly();
    public IReadOnlyCollection<Media> Media => _media.AsReadOnly();

    public static Place Create(string name, decimal lat, decimal lng, string timezone, UserId ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be empty.", nameof(timezone));

        // Validate coordinates eagerly (delegates to Coordinates value object rules)
        if (lat < -90m || lat > 90m)
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be between -90 and 90.");
        if (lng < -180m || lng > 180m)
            throw new ArgumentOutOfRangeException(nameof(lng), "Longitude must be between -180 and 180.");

        return new Place
        {
            Id = new PlaceId(Guid.NewGuid()),
            Name = name.Trim(),
            Latitude = lat,
            Longitude = lng,
            H3IndexRes8 = string.Empty, // populated by infrastructure before persistence
            H3IndexRes5 = string.Empty,
            Timezone = timezone,
            Source = ListingSourceType.User,
            Status = ListingStatus.Draft,
            OwnerId = ownerId
        };
    }

    public void Approve(UserId reviewerId)
    {
        if (Status != ListingStatus.PendingReview)
            throw new InvalidOperationException("Only listings in PendingReview status can be approved.");

        Status = ListingStatus.Live;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        IsVerified = true;
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
            throw new InvalidOperationException("A place cannot have more than 5 categories.");
        if (_categories.Any(c => c.Id == category.Id))
            return;

        _categories.Add(category);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Submit()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException("Only Draft places can be submitted for review.");

        Status = ListingStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestReview()
    {
        Status = ListingStatus.PendingReview;
        ReviewedBy = null;
        ReviewedAt = null;
        IsVerified = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetName(string name)
    {
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    // Records where a listing came from (OSM/Google import vs. user submission)
    public void SetSource(ListingSourceType source, string? externalSourceId)
    {
        Source = source;
        ExternalSourceId = externalSourceId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCoordinates(decimal lat, decimal lng, string h3Res8, string h3Res5)
    {
        Latitude = lat;
        Longitude = lng;
        H3IndexRes8 = h3Res8;
        H3IndexRes5 = h3Res5;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAddress(string? address, string? city, string? country, string? countryCode)
    {
        Address = address;
        City = city;
        Country = country;
        CountryCode = countryCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTimezone(string timezone)
    {
        Timezone = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetIsPermanent(bool isPermanent)
    {
        IsPermanent = isPermanent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCategories()
    {
        _categories.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddExternalLink(PlaceExternalLink link)
    {
        _externalLinks.Add(link);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearExternalLinks()
    {
        _externalLinks.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == ListingStatus.Archived)
            return;

        Status = ListingStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        SoftDelete();
    }
}
