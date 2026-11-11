import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

const AUTH_PATH = '/auth/';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const isOwnApi = request.url.startsWith(environment.apiUrl);
    if (!isOwnApi) return next.handle(request);

    const token = this.authService.getToken();
    const isAuthCall = request.url.startsWith(environment.apiUrl + AUTH_PATH);

    return next.handle(this.withToken(request, token)).pipe(
      catchError((error: HttpErrorResponse) => {
        // Access tokens last minutes; renew once and resend so a long form fill isn't lost
        if (error.status !== 401 || !token || isAuthCall || !this.authService.getRefreshToken()) {
          return throwError(() => error);
        }
        return this.authService.refreshAccessToken().pipe(
          catchError(() => throwError(() => error)),
          switchMap(freshToken => next.handle(this.withToken(request, freshToken)))
        );
      })
    );
  }

  private withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
    return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  }
}
