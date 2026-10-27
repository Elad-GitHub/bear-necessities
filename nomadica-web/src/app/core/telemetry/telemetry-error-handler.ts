import { ErrorHandler, Injectable, Injector } from '@angular/core';
import { TelemetryService } from './telemetry.service';

/**
 * Reports uncaught errors to telemetry, then preserves Angular's default console behaviour.
 * TelemetryService is resolved lazily via Injector to avoid a construction cycle
 * (ErrorHandler is created very early in bootstrap).
 */
@Injectable()
export class TelemetryErrorHandler implements ErrorHandler {
  constructor(private injector: Injector) {}

  handleError(error: unknown): void {
    try {
      this.injector.get(TelemetryService).trackError(error);
    } catch {
      // Never let a telemetry failure mask the original error
    }
    console.error(error);
  }
}
