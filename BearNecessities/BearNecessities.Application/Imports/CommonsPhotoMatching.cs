using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BearNecessities.Application.Imports;

/// <summary>A Wikimedia Commons file found near a place, before we decide whether it shows that place.</summary>
public sealed record CommonsPhotoCandidate(string FileTitle, string ImageUrl, string PageUrl, string? Artist, string? License);

/// <summary>A photo we may show for a place, with the credit its licence asks for.</summary>
public sealed record PlacePhoto(string Url, string Credit, string CreditUrl);

/// <summary>
/// Picks a photo of a place from the Commons files taken near it: the file name has to share a distinctive word
/// with the place name ("Osho" in "2008 12 Osho center, Pune"), and the licence has to allow reuse with credit.
/// </summary>
public static partial class CommonsPhotoMatching
{
    private const int MinWordLength = 4;
    private const int MaxArtistLength = 60;

    // Words that appear in many place names and many photo titles, so sharing one says nothing about the place
    private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal)
    {
        "restaurant", "restaurante", "cafe", "coffee", "kitchen", "food", "foods", "vegan", "vegetarian", "veggie",
        "organic", "natural", "health", "healthy", "yoga", "studio", "meditation", "center", "centre", "centro",
        "house", "casa", "home", "garden", "gardens", "market", "mercado", "shop", "store", "bakery", "hostel",
        "hotel", "resort", "camping", "campsite", "camp", "park", "beach", "street", "road", "avenida", "rua",
        "plaza", "praca", "international", "community", "school", "temple", "spring", "springs", "municipal",
        "feira", "fair", "station", "village", "city", "town"
    };

    private static readonly HashSet<string> JoinerWords = new(StringComparer.Ordinal)
    {
        "with", "from", "and", "the", "das", "dos", "del", "della", "des", "der", "die", "les", "los", "las"
    };

    private static readonly string[] FreeLicensePrefixes = ["CC BY", "CC0", "Public domain", "PD"];
    private static readonly string[] PhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>
    /// The nearest file taken near the place whose title holds every word of the place name
    /// ("Mercado Municipal de Santo Amaro em Lagos" for "Mercado Municipal de Santo Amaro"). Sharing only some
    /// words picks neighbours: "Guia Post Office" for "Mercado da Guia", the basilica for "Sagrada Familia Tantra Temple".
    /// </summary>
    public static PlacePhoto? PickPhoto(string placeName, string? city, IEnumerable<CommonsPhotoCandidate> candidates)
    {
        var nameWords = NameWords(placeName);
        if (nameWords.Count == 0 || DistinctiveWords(placeName, city).Count == 0) return null;
        return PickFirst(candidates, titleWords => nameWords.All(titleWords.Contains));
    }

    /// <summary>
    /// The best file from a text search, where files can come from anywhere: the title must hold the name's
    /// distinctive words and the city ("2008 12 Osho center, Pune" for "Osho Meditation Resort" in Pune).
    /// </summary>
    public static PlacePhoto? PickPhotoFromSearch(string placeName, string? city, IEnumerable<CommonsPhotoCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var distinctiveWords = DistinctiveWords(placeName, city);
        if (distinctiveWords.Count == 0) return null;
        var required = distinctiveWords.Concat(Words(city)).ToList();
        return PickFirst(candidates, titleWords => required.All(titleWords.Contains));
    }

    private static PlacePhoto? PickFirst(IEnumerable<CommonsPhotoCandidate> candidates, Func<HashSet<string>, bool> titleMatches)
    {
        var match = candidates.FirstOrDefault(candidate =>
            IsPhotoFile(candidate.FileTitle) && IsFreeLicense(candidate.License) && titleMatches(TitleWords(candidate.FileTitle)));
        return match is null ? null : new PlacePhoto(match.ImageUrl, BuildCredit(match), match.PageUrl);
    }

    public static bool IsFreeLicense(string? license) =>
        !string.IsNullOrWhiteSpace(license)
        && FreeLicensePrefixes.Any(prefix => license.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        && !license.Contains("NC", StringComparison.Ordinal)
        && !license.Contains("ND", StringComparison.Ordinal);

    private static bool IsPhotoFile(string fileTitle) =>
        PhotoExtensions.Any(extension => fileTitle.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> DistinctiveWords(string placeName, string? city)
    {
        var cityWords = city is null ? [] : Words(city).ToHashSet(StringComparer.Ordinal);
        return Words(placeName)
            .Where(word => word.Length >= MinWordLength && !GenericWords.Contains(word) && !JoinerWords.Contains(word)
                && !cityWords.Contains(word))
            .ToHashSet(StringComparer.Ordinal);
    }

    // Every meaningful word of the name; short joiners like "de", "da", "das" may be spelled differently in titles
    private static List<string> NameWords(string placeName) =>
        Words(placeName).Where(word => word.Length >= MinWordLength && !JoinerWords.Contains(word)).Distinct().ToList();

    private static HashSet<string> TitleWords(string fileTitle) =>
        Words(Path.GetFileNameWithoutExtension(fileTitle.Replace("File:", string.Empty))).ToHashSet(StringComparer.Ordinal);

    private static string BuildCredit(CommonsPhotoCandidate candidate)
    {
        // Artist is HTML and sometimes carries a whole note ("Kolforn (Kolforn)\nI'd appreciate if you could mail me...")
        var artist = string.IsNullOrWhiteSpace(candidate.Artist) ? null
            : TalkLink().Replace(HtmlTag().Replace(candidate.Artist, string.Empty), string.Empty).Split('\n')[0].Trim();
        if (artist is { Length: > MaxArtistLength }) artist = artist[..MaxArtistLength].TrimEnd() + "…";
        var author = string.IsNullOrWhiteSpace(artist) ? "Wikimedia Commons" : artist;
        return $"Photo: {author} · {candidate.License}";
    }

    // Lower case, accents dropped ("Praça" -> "praca"), split on anything that isn't a letter or digit
    private static IEnumerable<string> Words(string text)
    {
        var withoutAccents = new StringBuilder();
        foreach (var character in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                withoutAccents.Append(char.ToLowerInvariant(character));
        return NonWordCharacters().Split(withoutAccents.ToString()).Where(word => word.Length > 0);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s*\((talk|contribs)\)", RegexOptions.IgnoreCase)]
    private static partial Regex TalkLink();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordCharacters();
}
