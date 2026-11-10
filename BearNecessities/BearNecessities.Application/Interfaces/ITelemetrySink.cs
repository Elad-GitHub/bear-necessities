using BearNecessities.Application.Telemetry;

namespace BearNecessities.Application.Interfaces;

/// The single seam for where telemetry goes. The default implementation writes structured
/// logs (Serilog → console/file), and can be replaced or joined by a Seq/Sentry/PostHog sink
/// without changing any caller.
public interface ITelemetrySink
{
    Task TrackBatchAsync(IReadOnlyList<TelemetryEvent> events, CancellationToken cancellationToken = default);
}
