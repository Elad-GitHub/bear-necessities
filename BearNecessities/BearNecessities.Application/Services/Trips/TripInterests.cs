using System.Text.RegularExpressions;
using BearNecessities.Domain.Entities;

namespace BearNecessities.Application.Services.Trips;

/// <summary>
/// What a traveller is after: the subcategories they picked plus those their free-text wish names
/// ("quiet yoga days, vegan food, no parties"), and the subcategories the wish rules out.
/// </summary>
public sealed partial class TripInterests
{
    // Everyday words for subcategories whose names travellers rarely type
    private static readonly Dictionary<string, string[]> WordToSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["quiet"] = ["meditation", "vipassana", "yoga"],
        ["calm"] = ["meditation", "vipassana", "yoga"],
        ["peaceful"] = ["meditation", "vipassana", "yoga"],
        ["relax"] = ["meditation", "hot-springs", "thai-massage"],
        ["silent"] = ["vipassana", "meditation"],
        ["vegan"] = ["vegan-restaurant"],
        ["vegetarian"] = ["vegan-restaurant"],
        ["food"] = ["vegan-restaurant", "health-food-cafe"],
        ["cooking"] = ["food-workshop"],
        ["dance"] = ["ecstatic-dance", "contact-improv"],
        ["dancing"] = ["ecstatic-dance", "contact-improv"],
        ["music"] = ["live-music", "kirtan", "drumming-circle"],
        ["party"] = ["dj-set"],
        ["parties"] = ["dj-set"],
        ["nightlife"] = ["dj-set", "live-music"],
        ["nature"] = ["hiking", "hot-springs", "camping", "eco-village"],
        ["hike"] = ["hiking"],
        ["trek"] = ["hiking"],
        ["mountains"] = ["hiking"],
        ["healing"] = ["sound-healing", "reiki", "energy-healing"],
        ["massage"] = ["thai-massage", "lomi-lomi", "shiatsu", "watsu"],
        ["bodywork"] = ["thai-massage", "lomi-lomi", "shiatsu", "watsu"],
        ["volunteer"] = ["wwoofing", "permaculture", "environmental-work", "community-garden"],
        ["volunteering"] = ["wwoofing", "permaculture", "environmental-work", "community-garden"],
        ["farm"] = ["farm-stay", "wwoofing", "permaculture"],
        ["crafts"] = ["pottery", "woodworking", "painting", "macrame-fiber-arts", "jewelry-making"],
        ["art"] = ["painting", "pottery", "macrame-fiber-arts"],
        ["market"] = ["hippie-market", "artisan-market", "farmers-market"],
        ["markets"] = ["hippie-market", "artisan-market", "farmers-market"],
        ["shopping"] = ["hippie-market", "artisan-market", "tribal-clothing", "spiritual-supplies"],
        ["work"] = ["coworking-space", "co-living"],
        ["remote"] = ["coworking-space", "co-living"],
        ["nomad"] = ["coworking-space", "co-living"],
        ["ceremony"] = ["sacred-ceremony", "cacao-ceremony", "sweat-lodge", "shamanism"],
        ["ceremonies"] = ["sacred-ceremony", "cacao-ceremony", "sweat-lodge", "shamanism"],
        ["spiritual"] = ["meditation", "sacred-ceremony", "kirtan"],
        ["meditation"] = ["meditation", "vipassana"],
        ["meditate"] = ["meditation", "vipassana"],
        ["culture"] = ["sacred-ceremony", "painting", "woodworking", "artisan-market"],
        ["tibetan"] = ["meditation", "sacred-ceremony", "herbalism", "spiritual-supplies"],
        ["buddhist"] = ["meditation", "vipassana", "sacred-ceremony"],
        ["ice"] = ["cold-therapy"],
        ["cold"] = ["cold-therapy"],
        ["breathing"] = ["breathwork"],
        ["springs"] = ["hot-springs"],
        ["camping"] = ["camping"],
        ["circus"] = ["circus-arts"],
        ["acro"] = ["acro-yoga"],
        ["fasting"] = ["nutrition"],
        ["detox"] = ["nutrition"]
    };

    private static readonly HashSet<string> NegationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "no", "not", "without", "avoid", "skip", "except"
    };

    // Short words that would match too much ("art" inside "martial") are only used through WordToSlugs
    private const int MinDirectMatchLength = 4;

    public IReadOnlyList<Guid> Wanted { get; }
    public IReadOnlySet<Guid> RuledOut { get; }

    private readonly Dictionary<Guid, Category> _categoriesById;

    private TripInterests(IReadOnlyList<Guid> wanted, IReadOnlySet<Guid> ruledOut, Dictionary<Guid, Category> categoriesById)
    {
        Wanted = wanted;
        RuledOut = ruledOut;
        _categoriesById = categoriesById;
    }

    public bool IsEmpty => Wanted.Count == 0;

    public static TripInterests From(IReadOnlyCollection<Place> candidates, IReadOnlyList<Guid> pickedCategoryIds, string? wish)
    {
        var categoriesById = candidates
            .SelectMany(place => place.Categories)
            .GroupBy(category => category.Id.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var wanted = pickedCategoryIds.Distinct().ToList();
        var ruledOut = new HashSet<Guid>();

        var words = WordPattern().Matches(wish ?? string.Empty).Select(match => match.Value.ToLowerInvariant()).ToList();
        for (var i = 0; i < words.Count; i++)
        {
            var matches = CategoriesNamedBy(words[i], categoriesById.Values).Select(category => category.Id.Value);
            // "no parties", "without crowds" - a negation up to two words before rules the subcategory out
            var isNegated = (i > 0 && NegationWords.Contains(words[i - 1])) || (i > 1 && NegationWords.Contains(words[i - 2]));
            foreach (var categoryId in matches)
            {
                if (isNegated) ruledOut.Add(categoryId);
                else if (!wanted.Contains(categoryId)) wanted.Add(categoryId);
            }
        }

        wanted.RemoveAll(ruledOut.Contains);
        return new TripInterests(wanted, ruledOut, categoriesById);
    }

    public int MatchCount(Place place) =>
        place.Categories.Count(category => Wanted.Contains(category.Id.Value));

    public bool IsRuledOut(Place place) =>
        place.Categories.Any(category => RuledOut.Contains(category.Id.Value));

    /// <summary>
    /// The kinds of activity a place counts as for this traveller: its wanted subcategories,
    /// or all of its subcategories when the traveller named none. A day holds at most one of each kind.
    /// </summary>
    public IReadOnlyList<Guid> KindsOf(Place place) =>
        place.Categories
            .Select(category => category.Id.Value)
            .Where(categoryId => IsEmpty || Wanted.Contains(categoryId))
            .ToList();

    /// <summary>Names of the wanted subcategories a place offers, in the traveller's order of interest.</summary>
    public IReadOnlyList<string> MatchedNames(Place place) =>
        Wanted.Where(id => place.Categories.Any(category => category.Id.Value == id))
            .Select(id => _categoriesById.TryGetValue(id, out var category) ? category.Name : null)
            .OfType<string>()
            .ToList();

    // A word with its own entry means exactly that ("food" is vegan/health food, not every "... Food" subcategory)
    private static IEnumerable<Category> CategoriesNamedBy(string word, IEnumerable<Category> categories)
    {
        if (WordToSlugs.TryGetValue(word, out var slugs))
            return categories.Where(category => slugs.Contains(category.Slug));

        return word.Length < MinDirectMatchLength
            ? []
            : categories.Where(category => category.Slug.Split('-').Contains(word) || NameWords(category.Name).Contains(word));
    }

    private static IEnumerable<string> NameWords(string name) =>
        WordPattern().Matches(name).Select(match => match.Value.ToLowerInvariant());

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex WordPattern();
}
