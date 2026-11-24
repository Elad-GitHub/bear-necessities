using BearNecessities.Application.Imports;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Infrastructure.Imports;

/// Turns one tagged OSM object into an import record; shared by the Overpass and the PBF-file importers.
public static class OsmPlaceMapper
{
    public static ImportPlace? ToImportPlace(
        IReadOnlyDictionary<string, string> tags,
        string elementType,
        long elementId,
        decimal latitude,
        decimal longitude,
        string iso,
        string? countryName)
    {
        var name = Tag(tags, "name:en") ?? Tag(tags, "name");
        if (name is null)
            return null;

        var slugs = OsmCategoryRules.Classify(tags);
        if (slugs.Count == 0)
            return null;

        return new ImportPlace(
            Name: name,
            Latitude: latitude,
            Longitude: longitude,
            CategorySlugs: slugs.ToList(),
            Source: ListingSourceType.Osm,
            ExternalSourceId: $"osm:{elementType}/{elementId}",
            // Most OSM places have no description; a summary built from their tags fills the hover card
            Description: Tag(tags, "description") ?? OsmPlaceSummary.Build(tags, slugs[0]),
            Address: BuildAddress(tags),
            City: Tag(tags, "addr:city"),
            Country: countryName ?? iso,
            CountryCode: iso.ToUpperInvariant(),
            Website: Tag(tags, "website") ?? Tag(tags, "contact:website"),
            OpeningHours: Tag(tags, "opening_hours"),
            IsPermanent: true);
    }

    private static string? BuildAddress(IReadOnlyDictionary<string, string> tags)
    {
        var street = Tag(tags, "addr:street");
        if (street is null) return null;
        var houseNumber = Tag(tags, "addr:housenumber");
        return houseNumber is null ? street : $"{street} {houseNumber}";
    }

    private static string? Tag(IReadOnlyDictionary<string, string> tags, string key) =>
        tags.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
