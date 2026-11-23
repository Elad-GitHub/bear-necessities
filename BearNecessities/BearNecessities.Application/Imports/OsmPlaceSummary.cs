using BearNecessities.Application.Common;

namespace BearNecessities.Application.Imports;

/// <summary>
/// Writes a one-line description for an OpenStreetMap place from its tags, for the many places that have no
/// description of their own: "Fully vegan restaurant · Indian cuisine · Organic · Open Mo-Sa 12:00-22:00".
/// </summary>
public static class OsmPlaceSummary
{
    private const string Separator = " · ";
    private const int MaxOpeningHoursLength = 45;

    private static readonly Dictionary<string, string> KindBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["yoga"] = "Yoga studio",
        ["indian-food"] = "Indian restaurant",
        ["health-food-cafe"] = "Health food shop",
        ["camping"] = "Campsite",
        ["hot-springs"] = "Natural hot spring",
        ["farmers-market"] = "Market",
        ["community-garden"] = "Community garden",
        ["herbalism"] = "Herbal shop",
        ["spiritual-supplies"] = "Spiritual shop",
        ["ayurveda"] = "Ayurveda centre",
        ["acupuncture"] = "Acupuncture",
        ["reiki"] = "Reiki",
        ["thai-massage"] = "Thai massage",
        ["watsu"] = "Watsu",
        ["lomi-lomi"] = "Lomi Lomi massage",
        ["shiatsu"] = "Shiatsu",
        ["energy-healing"] = "Energy healing",
        ["vipassana"] = "Vipassana centre",
        ["meditation"] = "Meditation centre",
        ["retreat"] = "Retreat",
        ["eco-village"] = "Eco-village",
        ["permaculture"] = "Permaculture project",
        ["farm-stay"] = "Farm stay",
        ["sound-healing"] = "Sound healing",
        ["kirtan"] = "Kirtan",
        ["tantra"] = "Tantra centre",
        ["ecstatic-dance"] = "Ecstatic dance",
        ["cacao-ceremony"] = "Cacao ceremony",
        ["sweat-lodge"] = "Temazcal",
        ["hippie-market"] = "Hippie market",
        ["artisan-market"] = "Artisan market",
        ["coworking-space"] = "Co-working space",
        ["maker-space"] = "Maker space",
        ["tai-chi"] = "Tai chi",
        ["qigong"] = "Qigong",
        ["pottery"] = "Pottery studio",
        ["live-music"] = "Live music venue",
        ["breathwork"] = "Breathwork",
        ["acro-yoga"] = "Acro yoga",
        ["contact-improv"] = "Contact improvisation",
        ["circus-arts"] = "Circus school",
        ["kambo"] = "Kambo",
        ["shamanism"] = "Shamanic practice",
        ["wwoofing"] = "WWOOF farm",
        ["co-living"] = "Co-living",
        ["cold-therapy"] = "Ice bath",
        ["tuina"] = "Tuina",
        ["drumming-circle"] = "Drum circle",
        ["macrame-fiber-arts"] = "Weaving & fiber arts",
        ["shibari"] = "Shibari"
    };

    public static string? Build(IReadOnlyDictionary<string, string> tags, string categorySlug)
    {
        var parts = new List<string> { DescribeKind(tags, categorySlug) };

        var cuisine = DescribeCuisine(tags, parts[0]);
        if (cuisine is not null) parts.Add(cuisine);
        if (IsYesOrOnly(tags, "organic")) parts.Add("Organic");
        if (Is(tags, "tents", "yes") && Is(tags, "caravans", "yes")) parts.Add("Tents & caravans");
        else if (Is(tags, "tents", "yes")) parts.Add("Tents");
        if (Is(tags, "fee", "no")) parts.Add("Free entry");
        if (Is(tags, "takeaway", "yes")) parts.Add("Takeaway");
        if (Is(tags, "outdoor_seating", "yes")) parts.Add("Outdoor seating");

        // Long opening-hours rules read badly in a hover card; only short ones are worth showing
        if (tags.TryGetValue("opening_hours", out var hours) && hours.Length <= MaxOpeningHoursLength)
            parts.Add($"Open {hours.Replace(";", ",")}");

        return TextShortening.Shorten(string.Join(Separator, parts));
    }

    private static string DescribeKind(IReadOnlyDictionary<string, string> tags, string categorySlug)
    {
        if (KindBySlug.TryGetValue(categorySlug, out var kind))
            return kind;

        var venue = Is(tags, "amenity", "cafe") ? "café" : "restaurant";
        if (Is(tags, "diet:vegan", "only")) return $"Fully vegan {venue}";
        var cuisine = tags.GetValueOrDefault("cuisine", string.Empty);
        if (cuisine.Contains("vegan", StringComparison.OrdinalIgnoreCase)) return Capitalize($"vegan {venue}");
        if (cuisine.Contains("vegetarian", StringComparison.OrdinalIgnoreCase) || Is(tags, "diet:vegetarian", "only"))
            return Capitalize($"vegetarian {venue}");
        if (Is(tags, "diet:vegan", "yes")) return Capitalize($"{venue} with vegan options");
        return Capitalize(venue);
    }

    // "indian;vegetarian" -> "Indian cuisine", leaving out styles the kind already names;
    // when the kind names one ("Indian restaurant"), the rest read as extras: "Also Italian cuisine"
    private static string? DescribeCuisine(IReadOnlyDictionary<string, string> tags, string kind)
    {
        if (!tags.TryGetValue("cuisine", out var cuisine)) return null;

        var allStyles = cuisine.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(style => style.Replace('_', ' '))
            .ToList();
        var styles = allStyles.Where(style => !kind.Contains(style, StringComparison.OrdinalIgnoreCase)).Take(3).ToList();
        if (styles.Count == 0) return null;

        var listed = string.Join(", ", styles.Select(Capitalize));
        var kindNamesACuisine = allStyles.Count > styles.Count && !kind.Contains("vegan", StringComparison.OrdinalIgnoreCase)
            && !kind.Contains("vegetarian", StringComparison.OrdinalIgnoreCase);
        return kindNamesACuisine ? $"Also {listed} cuisine" : $"{listed} cuisine";
    }

    private static bool Is(IReadOnlyDictionary<string, string> tags, string key, string value) =>
        tags.TryGetValue(key, out var actual) && string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);

    private static bool IsYesOrOnly(IReadOnlyDictionary<string, string> tags, string key) =>
        Is(tags, key, "yes") || Is(tags, key, "only");

    private static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
