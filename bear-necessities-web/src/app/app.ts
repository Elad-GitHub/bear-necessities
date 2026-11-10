import { Component, signal } from '@angular/core';
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

  constructor(router: Router, telemetry: TelemetryService) {
    telemetry.start();
    router.events
      .pipe(filter((routerEvent): routerEvent is NavigationEnd => routerEvent instanceof NavigationEnd))
      .subscribe(navigationEnd => telemetry.trackPageView(navigationEnd.urlAfterRedirects));
  }
}
