import { TelemetryService } from './telemetry.service';
import { TelemetryEvent } from './telemetry.models';
import { TelemetrySink } from './telemetry-sink';

class FakeSink implements TelemetrySink {
  events: TelemetryEvent[] = [];
  capture(event: TelemetryEvent): void {
    this.events.push(event);
  }
}

describe('TelemetryService', () => {
  it('dispatches events to every sink with a stable session id', () => {
    const first = new FakeSink();
    const second = new FakeSink();
    const service = new TelemetryService([first, second]);

    service.trackEvent('open_place', { placeId: '42' });

    expect(first.events.length).toBe(1);
    expect(second.events.length).toBe(1);
    expect(first.events[0].type).toBe('event');
    expect(first.events[0].name).toBe('open_place');
    expect(first.events[0].properties).toEqual({ placeId: '42' });
    expect(first.events[0].sessionId).toBeTruthy();
  });

  it('keeps the same session id across calls', () => {
    const sink = new FakeSink();
    const service = new TelemetryService([sink]);

    service.trackEvent('a');
    service.trackPageView('/map');

    expect(sink.events[1].type).toBe('pageview');
    expect(sink.events[0].sessionId).toBe(sink.events[1].sessionId);
  });

  it('stringifies non-string properties', () => {
    const sink = new FakeSink();
    const service = new TelemetryService([sink]);

    service.trackEvent('filter', { count: 3, active: true });

    expect(sink.events[0].properties).toEqual({ count: '3', active: 'true' });
  });

  it('records errors with name and message', () => {
    const sink = new FakeSink();
    const service = new TelemetryService([sink]);

    service.trackError(new Error('boom'));

    expect(sink.events[0].type).toBe('error');
    expect(sink.events[0].name).toBe('Error');
    expect(sink.events[0].properties?.['message']).toBe('boom');
  });

  it('is a safe no-op when no sinks are registered', () => {
    const service = new TelemetryService(null);
    expect(() => service.trackEvent('anything')).not.toThrow();
  });
});
