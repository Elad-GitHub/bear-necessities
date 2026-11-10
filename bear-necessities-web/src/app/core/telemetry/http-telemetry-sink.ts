import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TelemetryEvent } from './telemetry.models';
import { TelemetrySink } from './telemetry-sink';

/**
 * Default sink: buffers events and ships them to the backend in small batches
 * (on a timer, when the buffer fills, and on tab-hide) so it never blocks the UI.
 */
@Injectable({ providedIn: 'root' })
export class HttpTelemetrySink implements TelemetrySink {
  private readonly endpoint = `${environment.apiUrl}/telemetry/batch`;
  private readonly maxBuffer = 20;
  private readonly flushIntervalMs = 10000;
  private readonly http: HttpClient;
  private buffer: TelemetryEvent[] = [];

  constructor(backend: HttpBackend) {
    // Bypass interceptors: telemetry must not carry auth headers, and must not trip the
    // error interceptor (which would loop if the telemetry endpoint itself failed).
    this.http = new HttpClient(backend);

    if (typeof window !== 'undefined') {
      window.setInterval(() => this.flush(), this.flushIntervalMs);
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') this.flush(true);
      });
    }
  }

  capture(event: TelemetryEvent): void {
    this.buffer.push(event);
    if (this.buffer.length >= this.maxBuffer) this.flush();
  }

  flush(useBeacon = false): void {
    if (this.buffer.length === 0) return;
    const events = this.buffer;
    this.buffer = [];
    const body = JSON.stringify({ events });

    // sendBeacon survives page unload where a normal request would be cancelled
    if (useBeacon && typeof navigator !== 'undefined' && navigator.sendBeacon) {
      navigator.sendBeacon(this.endpoint, new Blob([body], { type: 'application/json' }));
      return;
    }

    this.http
      .post(this.endpoint, body, { headers: { 'Content-Type': 'application/json' } })
      .subscribe({ error: () => { /* drop telemetry on failure — never disrupt the app */ } });
  }
}
