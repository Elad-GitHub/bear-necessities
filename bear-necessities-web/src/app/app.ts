import { Component, DestroyRef, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { TelemetryService } from './core/telemetry/telemetry.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('bear-necessities-web');
  protected readonly isOffline = signal(typeof navigator !== 'undefined' && navigator.onLine === false);

  constructor(router: Router, telemetry: TelemetryService) {
    telemetry.start();
    router.events
      .pipe(filter((routerEvent): routerEvent is NavigationEnd => routerEvent instanceof NavigationEnd))
      .subscribe(navigationEnd => telemetry.trackPageView(navigationEnd.urlAfterRedirects));
    this.watchConnection();
  }

  private watchConnection(): void {
    const goOffline = () => this.isOffline.set(true);
    const goOnline = () => this.isOffline.set(false);
    window.addEventListener('offline', goOffline);
    window.addEventListener('online', goOnline);
    inject(DestroyRef).onDestroy(() => {
      window.removeEventListener('offline', goOffline);
      window.removeEventListener('online', goOnline);
    });
  }
}
