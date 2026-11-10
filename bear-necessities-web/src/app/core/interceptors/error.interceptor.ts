import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { TelemetryService } from '../telemetry/telemetry.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private toastService: ToastService,
    private telemetry: TelemetryService
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        this.telemetry.trackEvent('http_error', {
          status: error.status,
          method: request.method,
          url: request.url
        });

        switch (error.status) {
          case 401:
            this.authService.logout();
            break;
          case 403:
            this.toastService.error("You don't have permission to perform this action");
            break;
          case 422:
            // Let component handle validation errors
            break;
          case 429:
            this.toastService.warning('Too many requests, please slow down');
            break;
          case 500:
            this.toastService.error('Something went wrong. Please try again.');
            break;
          default:
            if (error.status >= 400) {
              this.toastService.error(error.error?.message || 'An error occurred');
            }
        }

        return throwError(() => error);
      })
    );
  }
}
