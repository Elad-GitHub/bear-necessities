using System.Text.Json;

namespace Nomadica.Application.Common;

public static class ChangeLogReasonParser
{
    public static string? ParseReason(string? newDataJson)
    {
        if (string.IsNullOrEmpty(newDataJson)) return null;

        try
        {
            using var document = JsonDocument.Parse(newDataJson);
            return document.RootElement.TryGetProperty("Reason", out var reason)
                ? reason.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
