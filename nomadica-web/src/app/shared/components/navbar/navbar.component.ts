import { Component, signal, ElementRef, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, forkJoin } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { GeocodingService, GeocodeResult } from '../../../core/services/geocoding.service';
import { PlaceSummaryDto } from '../../../core/models/place.model';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  standalone: false
})
export class NavbarComponent {
  destinationResults = signal<GeocodeResult[]>([]);
  placeResults = signal<PlaceSummaryDto[]>([]);
  isSearchOpen = signal(false);
  isSearching = signal(false);

  private searchTerms = new Subject<string>();

  constructor(
    public authService: AuthService,
    private api: ApiService,
    private geocodingService: GeocodingService,
    private elementRef: ElementRef,
    private router: Router
  ) {
    this.searchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        if (term.trim().length < 2) {
          this.isSearching.set(false);
          return of({ destinations: [] as GeocodeResult[], places: { items: [] as PlaceSummaryDto[] } });
        }
        this.isSearching.set(true);
        // Destinations (cities/countries) and named places, in parallel
        return forkJoin({
          destinations: this.geocodingService.search(term, 4),
          places: this.api.get<{ items: PlaceSummaryDto[] }>('/places/search', { query: term, limit: 6 })
        });
      })
    ).subscribe({
      next: (response) => {
        this.destinationResults.set(response.destinations || []);
        this.placeResults.set(response.places?.items || []);
        this.isSearching.set(false);
        this.isSearchOpen.set(true);
      },
      error: () => this.isSearching.set(false)
    });
  }

  hasResults(): boolean {
    return this.destinationResults().length > 0 || this.placeResults().length > 0;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(clickEvent: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(clickEvent.target)) {
      this.isSearchOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isSearchOpen.set(false);
  }

  onSearchInput(term: string): void {
    this.searchTerms.next(term);
  }

  // A destination flies the map to that area (country bbox or city center)
  openDestination(destination: GeocodeResult, searchInput: HTMLInputElement): void {
    this.clearSearch(searchInput);
    const params: Record<string, string> = {
      flyLng: String(destination.longitude),
      flyLat: String(destination.latitude)
    };
    if (destination.bbox) params['flyBbox'] = destination.bbox.join(',');
    this.router.navigate(['/map'], { queryParams: params });
  }

  openSearchResult(place: PlaceSummaryDto, searchInput: HTMLInputElement): void {
    this.clearSearch(searchInput);
    // Focus the place on the map (zoom + tooltip) instead of opening its detail page
    this.router.navigate(['/map'], { queryParams: { focus: place.id } });
  }

  private clearSearch(searchInput: HTMLInputElement): void {
    this.isSearchOpen.set(false);
    searchInput.value = '';
    this.destinationResults.set([]);
    this.placeResults.set([]);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  openTripPlanner(): void {
    this.router.navigate(['/map'], { queryParams: { plan: 1 } });
  }

  navigateToProfileTab(tab: 'rsvps' | 'saved' | 'listings'): void {
    this.router.navigate(['/profile'], { queryParams: { tab } });
  }

  isAdmin(): boolean {
    return (this.authService.currentUser()?.roles || []).includes('Admin');
  }

  getInitials(): string {
    const user = this.authService.currentUser();
    if (!user?.displayName) return '';
    const parts = user.displayName.split(' ');
    return parts.map(p => p[0]).join('').toUpperCase().slice(0, 2);
  }

  logout(): void {
    this.authService.logout();
  }
}
