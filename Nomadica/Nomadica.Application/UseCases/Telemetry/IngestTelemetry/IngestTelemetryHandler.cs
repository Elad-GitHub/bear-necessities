using Nomadica.Application.Interfaces;
using Nomadica.Application.Telemetry;

namespace Nomadica.Application.UseCases.Telemetry.IngestTelemetry;

public sealed class IngestTelemetryHandler
{
    private const int MaxEventsPerBatch = 50;
    private const int MaxNameLength = 200;
    private const int MaxPropertyEntries = 20;
    private const int MaxPropertyValueLength = 500;

    private readonly ITelemetrySink _sink;
    private readonly ICurrentUserService _currentUser;

    public IngestTelemetryHandler(ITelemetrySink sink, ICurrentUserService currentUser)
    {
        _sink = sink;
        _currentUser = currentUser;
    }

    public async Task<IngestTelemetryResponse> HandleAsync(
        IngestTelemetryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Events is null || request.Events.Count == 0)
            return new IngestTelemetryResponse(0);

        // Identity and time are stamped server-side — the client is never trusted for either.
        var userId = _currentUser.UserId?.Value;
        var receivedAt = DateTime.UtcNow;

        var events = request.Events
            .Take(MaxEventsPerBatch)
            .Select(incoming => new TelemetryEvent
            {
                Type = Truncate(NullOrWhiteSpaceTo(incoming.Type, "event"), 40),
                Name = Truncate(incoming.Name ?? string.Empty, MaxNameLength),
                Url = incoming.Url is null ? null : Truncate(incoming.Url, MaxNameLength),
                SessionId = incoming.SessionId is null ? null : Truncate(incoming.SessionId, 64),
                UserId = userId,
                TimestampUtc = receivedAt,
                Properties = Sanitize(incoming.Properties)
            })
            .ToList();

        await _sink.TrackBatchAsync(events, cancellationToken);
        return new IngestTelemetryResponse(events.Count);
    }

    private static IReadOnlyDictionary<string, string>? Sanitize(IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null || properties.Count == 0) return null;
        return properties
            .Take(MaxPropertyEntries)
            .ToDictionary(
                pair => Truncate(pair.Key, 60),
                pair => Truncate(pair.Value ?? string.Empty, MaxPropertyValueLength));
    }

    private static string NullOrWhiteSpaceTo(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
