using Microsoft.Extensions.Logging;
using Nomadica.Application.Interfaces;
using Nomadica.Application.Telemetry;

namespace Nomadica.Infrastructure.Telemetry;

/// Default telemetry destination: emits structured records through <see cref="ILogger"/>,
/// which Serilog routes to the console and rolling files today. Pointing this at Seq (local)
/// or Sentry/PostHog (production) is a Serilog-sink/config change — callers stay untouched.
public sealed class SerilogTelemetrySink : ITelemetrySink
{
    private readonly ILogger<SerilogTelemetrySink> _logger;

    public SerilogTelemetrySink(ILogger<SerilogTelemetrySink> logger) => _logger = logger;

    public Task TrackBatchAsync(IReadOnlyList<TelemetryEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var telemetryEvent in events)
        {
            _logger.LogInformation(
                "Telemetry {TelemetryType} {TelemetryName} user={TelemetryUserId} session={TelemetrySession} {@TelemetryProperties}",
                telemetryEvent.Type,
                telemetryEvent.Name,
                telemetryEvent.UserId,
                telemetryEvent.SessionId,
                telemetryEvent.Properties);
        }

        return Task.CompletedTask;
    }
}
