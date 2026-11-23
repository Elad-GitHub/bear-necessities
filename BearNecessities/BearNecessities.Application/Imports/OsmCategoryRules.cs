using System.Text.RegularExpressions;

namespace BearNecessities.Application.Imports;

/// <summary>
/// Decides which subcategories an OpenStreetMap place belongs to, from its tags and, for places that aren't
/// somewhere to eat, from telling words in its name ("Dhamma Giri" is a Vipassana centre). Returns up to five
/// subcategory slugs, the strongest first; an empty list means the place doesn't belong on the map.
/// </summary>
public static partial class OsmCategoryRules
{
    public const int MaxCategories = 5;

    // Food places are named for atmosphere ("Tantra Beach Shack", "Nirvana Café"), so their names prove nothing
    private static readonly HashSet<string> FoodAmenities = new(StringComparer.OrdinalIgnoreCase)
    {
        "restaurant", "cafe", "fast_food", "bar", "pub", "food_court", "ice_cream", "biergarten"
    };

    // Name word (case-insensitive) -> subcategory, for places that aren't food places
    private static readonly (Regex Pattern, string Slug)[] NameRules =
    [
        (VipassanaName(), "vipassana"),
        (Word("meditation (centre|center|retreat|resort|hall|temple)|meditation$"), "meditation"),
        (Word("ayurved(a|ic)"), "ayurveda"),
        (Word("retreats?"), "retreat"),
        (Word("ecstatic dance"), "ecstatic-dance"),
        (Word("eco[- ]?village|ecovila|ecoaldea|ecoaldeia"), "eco-village"),
        (Word("permacultur(e|a)"), "permaculture"),
        (Word("farm ?stay|agroturismo|agriturismo|agritourism"), "farm-stay"),
        (Word("sound (healing|bath)|singing bowls?"), "sound-healing"),
        (Word("reiki"), "reiki"),
        (Word("kirtan"), "kirtan"),
        (Word("tantra (temple|yoga|school|institute|centre|center|retreat|massage|workshop)"), "tantra"),
        (Word("cacao ceremon(y|ies)"), "cacao-ceremony"),
        (Word("temazcal|sweat lodge"), "sweat-lodge"),
        (Word("yoga"), "yoga"),
        (Word("acupunct(ure|ura)"), "acupuncture"),
        (Word("thai massage"), "thai-massage"),
        (Word("qi ?gong"), "qigong"),
        (Word("tai ?chi"), "tai-chi"),
        (Word("breathwork"), "breathwork"),
        (Word("acro ?yoga"), "acro-yoga"),
        (Word("contact improv(isation)?"), "contact-improv"),
        (Word("circus school|circus arts"), "circus-arts"),
        // "Kambo" alone is also a surname and place name (Kambo Tailor, Buntu Kambo), so it needs a healing word beside it
        (Word("kambo (ceremon(y|ies)|healing|medicine|therapy|circle|practitioner)|frog medicine"), "kambo"),
        (Word("shaman(ic|ism)?"), "shamanism"),
        (Word("wwoof(ing)?"), "wwoofing"),
        (Word("co-?living"), "co-living"),
        (Word("ice bath|wim hof"), "cold-therapy"),
        (Word("tuina|tui na"), "tuina"),
        (Word("drum(ming)? circle"), "drumming-circle"),
        (Word("macram(e|é)"), "macrame-fiber-arts"),
        (Word("watsu"), "watsu"),
        (Word("lomi ?lomi"), "lomi-lomi"),
        (Word("shiatsu"), "shiatsu"),
        (Word("shibari"), "shibari"),
        (Word("fab ?lab|makerspace|maker space"), "maker-space")
    ];

    private static readonly (Regex Pattern, string Slug)[] MarketRules =
    [
        (Word("hippie|hippy|hipp(ie|y) market|bohemian|boho|tribal"), "hippie-market"),
        (Word("artisan(s|al)?|artesan(o|os|ia|ía|ato)|craft(s)?|handicraft(s)?|artisanat|feira de artesanato"), "artisan-market")
    ];

    public static IReadOnlyList<string> Classify(IReadOnlyDictionary<string, string> tags)
    {
        var slugs = new List<string>();
        void Add(string slug) { if (!slugs.Contains(slug)) slugs.Add(slug); }

        if (Is(tags, "sport", "yoga")) Add("yoga");

        var cuisine = tags.GetValueOrDefault("cuisine", string.Empty).ToLowerInvariant();
        if (cuisine.Contains("vegan") || cuisine.Contains("vegetarian") || IsYesOrOnly(tags, "diet:vegan")
            || Is(tags, "diet:vegetarian", "only"))
            Add("vegan-restaurant");
        if (cuisine.Contains("raw") || IsYesOrOnly(tags, "diet:raw")) Add("raw-food");
        if (cuisine.Contains("indian")) Add("indian-food");

        if (Is(tags, "shop", "health_food")) Add("health-food-cafe");
        if (Is(tags, "shop", "herbalist")) Add("herbalism");
        if (Is(tags, "shop", "esoteric")) Add("spiritual-supplies");

        AddHealing(tags, Add);

        if (Is(tags, "tourism", "camp_site")) Add("camping");
        if (Is(tags, "natural", "hot_spring")
            || (Is(tags, "amenity", "public_bath") && tags.GetValueOrDefault("bath:type", string.Empty)
                .Contains("hot_spring", StringComparison.OrdinalIgnoreCase))
            || (Is(tags, "amenity", "public_bath") && Is(tags, "bath:type", "onsen")))
            Add("hot-springs");
        if (Is(tags, "leisure", "garden") && Is(tags, "garden:type", "community")) Add("community-garden");
        if (Is(tags, "amenity", "coworking_space")) Add("coworking-space");
        if (Is(tags, "leisure", "hackerspace")) Add("maker-space");
        if (Is(tags, "sport", "tai_chi")) Add("tai-chi");
        if (Is(tags, "sport", "qigong")) Add("qigong");
        if (Is(tags, "craft", "pottery")) Add("pottery");
        if (Is(tags, "amenity", "music_venue")) Add("live-music");

        var name = tags.GetValueOrDefault("name", string.Empty);
        if (Is(tags, "amenity", "marketplace"))
        {
            var kind = MarketRules.FirstOrDefault(rule => rule.Pattern.IsMatch(name)).Slug;
            Add(kind ?? "farmers-market");
        }

        var isFoodPlace = tags.TryGetValue("amenity", out var amenity) && FoodAmenities.Contains(amenity);
        if (!isFoodPlace)
            foreach (var (pattern, slug) in NameRules)
                if (pattern.IsMatch(name)) Add(slug);

        return slugs.Take(MaxCategories).ToList();
    }

    // Alternative healthcare names its practice in healthcare:speciality ("ayurveda;acupuncture")
    private static void AddHealing(IReadOnlyDictionary<string, string> tags, Action<string> add)
    {
        var specialities = tags.GetValueOrDefault("healthcare:speciality", string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(speciality => speciality.ToLowerInvariant());
        foreach (var speciality in specialities)
        {
            switch (speciality)
            {
                case "ayurveda": add("ayurveda"); break;
                case "acupuncture": add("acupuncture"); break;
                case "reiki": add("reiki"); break;
                case "herbalism" or "naturopathy" or "phytotherapy": add("herbalism"); break;
                case "shiatsu": add("shiatsu"); break;
                case "energy_healing" or "bioenergy" or "pranic_healing": add("energy-healing"); break;
            }
        }

        // Only named styles belong on the map; a plain massage shop is usually an ordinary spa
        var isMassage = Is(tags, "shop", "massage") || Is(tags, "amenity", "massage") || Is(tags, "healthcare", "massage");
        if (!isMassage) return;
        var isThai = Is(tags, "massage", "thai")
            || tags.GetValueOrDefault("name", string.Empty).Contains("thai", StringComparison.OrdinalIgnoreCase);
        if (isThai) add("thai-massage");
    }

    private static bool Is(IReadOnlyDictionary<string, string> tags, string key, string value) =>
        tags.TryGetValue(key, out var actual) && string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);

    private static bool IsYesOrOnly(IReadOnlyDictionary<string, string> tags, string key) =>
        Is(tags, key, "yes") || Is(tags, key, "only");

    private static Regex Word(string alternatives) =>
        new($@"(?<![\p{{L}}])({alternatives})(?![\p{{L}}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Goenka centres are all named "Dhamma <name>"; others say vipassana outright
    [GeneratedRegex(@"^dhamma\s|vipassana", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VipassanaName();
}
