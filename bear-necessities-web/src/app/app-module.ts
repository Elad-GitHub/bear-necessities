import { NgModule, APP_INITIALIZER, ErrorHandler } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { MatSnackBarModule } from '@angular/material/snack-bar';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';
import { AuthService } from './core/services/auth.service';
import { CategoriesService } from './core/services/categories.service';
import { TELEMETRY_SINKS } from './core/telemetry/telemetry-sink';
import { HttpTelemetrySink } from './core/telemetry/http-telemetry-sink';
import { TelemetryErrorHandler } from './core/telemetry/telemetry-error-handler';

@NgModule({
  declarations: [
    App
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    MatSnackBarModule,
    AppRoutingModule
  ],
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => authService.loadFromStorage(),
      deps: [AuthService],
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: (categoriesService: CategoriesService) => () => categoriesService.loadCategories().toPromise(),
      deps: [CategoriesService],
      multi: true
    },
    // Error handling wraps auth so a 401 only logs out after AuthInterceptor failed to refresh the token
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    // Telemetry: the default HTTP sink now; add another sink (e.g. Sentry) here later
    // without touching TelemetryService or any call site.
    {
      provide: TELEMETRY_SINKS,
      useExisting: HttpTelemetrySink,
      multi: true
    },
    {
      provide: ErrorHandler,
      useClass: TelemetryErrorHandler
    }
  ],
  bootstrap: [App]
})
export class AppModule { }
