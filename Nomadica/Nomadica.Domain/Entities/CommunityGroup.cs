using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class CommunityGroup : BaseEntity<GroupId>
{
    private CommunityGroup() { }

    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public UserId CreatedBy { get; private set; } = default!;
    public int MemberCount { get; private set; }

    public static CommunityGroup Create(
        string name,
        decimal latitude,
        decimal longitude,
        UserId createdBy,
        string? description = null,
        string? city = null,
        string? country = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.", nameof(name));
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        return new CommunityGroup
        {
            Id = new GroupId(Guid.NewGuid()),
            Slug = BuildSlug(name),
            Name = name.Trim(),
            Description = description,
            City = city,
            Country = country,
            Latitude = latitude,
            Longitude = longitude,
            CreatedBy = createdBy,
            MemberCount = 0
        };
    }

    public void UpdateDetails(string? description, string? city, string? country)
    {
        Description = description;
        City = city;
        Country = country;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementMemberCount()
    {
        MemberCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementMemberCount()
    {
        if (MemberCount > 0) MemberCount--;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string BuildSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = new string(slug.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        // Suffix keeps slugs unique without a round-trip to the database
        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }
}
