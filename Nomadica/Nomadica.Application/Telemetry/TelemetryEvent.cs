namespace Nomadica.Application.Telemetry;

/// A single monitoring/analytics record (a page view, a UI event, or an error).
/// Provider-agnostic on purpose so the sink behind <see cref="Interfaces.ITelemetrySink"/> can change.
public sealed record TelemetryEvent
{
    public string Type { get; init; } = "event";
    public string Name { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? SessionId { get; init; }
    public Guid? UserId { get; init; }
    public DateTime TimestampUtc { get; init; }
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
