namespace BearNecessities.Application.UseCases.Telemetry.IngestTelemetry;

public sealed record ClientTelemetryEvent(
    string? Type,
    string? Name,
    string? Url,
    string? SessionId,
    IReadOnlyDictionary<string, string>? Properties);

public sealed record IngestTelemetryRequest(IReadOnlyList<ClientTelemetryEvent>? Events);
