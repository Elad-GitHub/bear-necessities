using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class PlaceExternalLink : BaseEntity<Guid>
{
    private PlaceExternalLink() { }

    public PlaceId PlaceId { get; private set; } = default!;
    public string Label { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public int SortOrder { get; private set; }

    public static PlaceExternalLink Create(PlaceId placeId, string label, string url, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be empty.", nameof(label));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        return new PlaceExternalLink
        {
            Id = Guid.NewGuid(),
            PlaceId = placeId,
            Label = label.Trim(),
            Url = url.Trim(),
            SortOrder = sortOrder
        };
    }
}
