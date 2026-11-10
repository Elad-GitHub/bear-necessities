export type TelemetryEventType = 'event' | 'error' | 'pageview';

export interface TelemetryEvent {
  type: TelemetryEventType;
  name: string;
  url: string;
  sessionId: string;
  timestampUtc: string;
  properties?: Record<string, string>;
}
