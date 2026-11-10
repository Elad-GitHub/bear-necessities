namespace BearNecessities.Application.Common;

public static class TextShortening
{
    /// <summary>Map hover cards show a blurb, not the full text; listing payloads stay small with thousands of pins.</summary>
    public const int SummaryDescriptionLength = 160;

    public static string? Shorten(string? text, int maxLength = SummaryDescriptionLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength].TrimEnd() + "…";
    }
}
