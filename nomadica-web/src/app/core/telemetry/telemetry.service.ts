import { Inject, Injectable, Optional } from '@angular/core';
import { TelemetryEvent, TelemetryEventType } from './telemetry.models';
import { TELEMETRY_SINKS, TelemetrySink } from './telemetry-sink';

/**
 * The one entry point the whole app calls to record behaviour and errors. It builds a
 * common event envelope (session, url, time) and fans out to every registered sink, so
 * changing where telemetry goes only means changing the sink registration.
 */
@Injectable({ providedIn: 'root' })
export class TelemetryService {
  private readonly sessionId = TelemetryService.newSessionId();
  private started = false;

  constructor(@Optional() @Inject(TELEMETRY_SINKS) private sinks: TelemetrySink[] | null) {}

  /** Wire global click capture. Called once from the root component. */
  start(): void {
    if (this.started || typeof document === 'undefined') return;
    this.started = true;
    // Capture phase so clicks are recorded even when a handler stops propagation
    document.addEventListener('click', event => this.onDocumentClick(event), { capture: true });
  }

  trackEvent(name: string, properties?: Record<string, unknown>): void {
    this.dispatch('event', name, properties);
  }

  trackPageView(url: string): void {
    this.dispatch('pageview', url);
  }

  trackError(error: unknown, context?: Record<string, unknown>): void {
    const failure = error as { name?: string; message?: string; stack?: string };
    this.dispatch('error', failure?.name || 'Error', {
      message: failure?.message ?? String(error),
      stack: failure?.stack,
      ...context
    });
  }

  private onDocumentClick(event: MouseEvent): void {
    const origin = event.target as HTMLElement | null;
    const actionable = origin?.closest('[data-track],button,a') as HTMLElement | null;
    if (!actionable) return;
    const label =
      actionable.getAttribute('data-track') ||
      actionable.getAttribute('aria-label') ||
      (actionable.textContent || '').replace(/\s+/g, ' ').trim().slice(0, 60);
    if (!label) return;
    this.trackEvent('click', { label, tag: actionable.tagName.toLowerCase() });
  }

  private dispatch(type: TelemetryEventType, name: string, properties?: Record<string, unknown>): void {
    if (!this.sinks || this.sinks.length === 0) return;
    const event: TelemetryEvent = {
      type,
      name,
      url: typeof location !== 'undefined' ? location.pathname : '',
      sessionId: this.sessionId,
      timestampUtc: new Date().toISOString(),
      properties: TelemetryService.stringifyProps(properties)
    };
    for (const sink of this.sinks) sink.capture(event);
  }

  private static stringifyProps(properties?: Record<string, unknown>): Record<string, string> | undefined {
    if (!properties) return undefined;
    const result: Record<string, string> = {};
    for (const [key, value] of Object.entries(properties)) {
      if (value === undefined || value === null) continue;
      result[key] = typeof value === 'string' ? value : JSON.stringify(value);
    }
    return Object.keys(result).length > 0 ? result : undefined;
  }

  private static newSessionId(): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) return crypto.randomUUID();
    return `sess-${Math.random().toString(36).slice(2)}${Date.now().toString(36)}`;
  }
}
