import { InjectionToken } from '@angular/core';
import { TelemetryEvent } from './telemetry.models';

/**
 * A destination for telemetry. Register one or many via the TELEMETRY_SINKS multi-token;
 * swapping providers (HTTP → Sentry/PostHog) never touches TelemetryService or call sites.
 */
export interface TelemetrySink {
  capture(event: TelemetryEvent): void;
  flush?(useBeacon?: boolean): void;
}

export const TELEMETRY_SINKS = new InjectionToken<TelemetrySink[]>('TELEMETRY_SINKS');
