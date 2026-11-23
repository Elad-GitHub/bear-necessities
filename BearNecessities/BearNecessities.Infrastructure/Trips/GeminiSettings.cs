namespace BearNecessities.Infrastructure.Trips;

/// <summary>"Gemini" configuration section. Without an ApiKey the trip planner never calls Gemini.</summary>
public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 20;
    // Calls allowed per minute across all users; past it the smart planner answers (keeps the free quota for everyone)
    public int MaxRequestsPerMinute { get; set; } = 10;
    // Places sent to the model, best matches first; keeps prompts small and fast
    public int MaxCandidates { get; set; } = 60;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);
}
