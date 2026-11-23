import { Component, signal, ElementRef, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, Subject, debounceTime, distinctUntilChanged, switchMap, map, of, forkJoin, catchError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { EventsService } from '../../../core/services/events.service';
import { GroupsService, GroupSummaryDto } from '../../../core/services/groups.service';
import { GeocodingService, GeocodeResult } from '../../../core/services/geocoding.service';
import { PlaceSummaryDto } from '../../../core/models/place.model';
import { EventSummaryDto } from '../../../core/models/event.model';

const MIN_SEARCH_LENGTH = 2;

interface SearchResults {
  destinations: GeocodeResult[];
  places: PlaceSummaryDto[];
  events: EventSummaryDto[];
  groups: GroupSummaryDto[];
}

const EMPTY_RESULTS: SearchResults = { destinations: [], places: [], events: [], groups: [] };

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  standalone: false
})
export class NavbarComponent {
  destinationResults = signal<GeocodeResult[]>([]);
  placeResults = signal<PlaceSummaryDto[]>([]);
  eventResults = signal<EventSummaryDto[]>([]);
  groupResults = signal<GroupSummaryDto[]>([]);
  isSearchOpen = signal(false);
  isSearching = signal(false);
  isPanelOpen = signal(false);

  private searchTerms = new Subject<string>();

  constructor(
    public authService: AuthService,
    private api: ApiService,
    private eventsService: EventsService,
    private groupsService: GroupsService,
    private geocodingService: GeocodingService,
    private elementRef: ElementRef,
    private router: Router
  ) {
    this.searchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => {
        if (term.trim().length < MIN_SEARCH_LENGTH) {
          this.isSearching.set(false);
          return of(EMPTY_RESULTS);
        }
        this.isSearching.set(true);
        return this.searchEverything(term.trim());
      })
    ).subscribe(results => {
      this.destinationResults.set(results.destinations);
      this.placeResults.set(results.places);
      this.eventResults.set(results.events);
      this.groupResults.set(results.groups);
      this.isSearching.set(false);
      this.isSearchOpen.set(true);
    });
  }

  // One failing source shouldn't blank the others or end the search stream
  private searchEverything(term: string): Observable<SearchResults> {
    const orEmpty = <T>(source: Observable<T[]>) => source.pipe(catchError(() => of([] as T[])));
    const itemsOf = <T>(source: Observable<{ items: T[] }>) =>
      orEmpty(source.pipe(map(response => response?.items || [])));

    return forkJoin({
      destinations: orEmpty(this.geocodingService.search(term, 3)),
      places: itemsOf(this.api.get<{ items: PlaceSummaryDto[] }>('/places/search', { query: term, limit: 5 })),
      events: itemsOf(this.eventsService.search(term, 4)),
      groups: itemsOf(this.groupsService.getGroups(term, 4))
    });
  }

  hasResults(): boolean {
    return this.destinationResults().length > 0
      || this.placeResults().length > 0
      || this.eventResults().length > 0
      || this.groupResults().length > 0;
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
    this.isPanelOpen.set(false);
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

  openEventResult(eventItem: EventSummaryDto, searchInput: HTMLInputElement): void {
    this.clearSearch(searchInput);
    this.router.navigate(['/events', eventItem.id]);
  }

  openGroupResult(group: GroupSummaryDto, searchInput: HTMLInputElement): void {
    this.clearSearch(searchInput);
    this.router.navigate(['/groups', group.id]);
  }

  private clearSearch(searchInput: HTMLInputElement): void {
    this.isSearchOpen.set(false);
    searchInput.value = '';
    this.destinationResults.set([]);
    this.placeResults.set([]);
    this.eventResults.set([]);
    this.groupResults.set([]);
  }

  openPanel(): void {
    this.isPanelOpen.set(true);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
  }

  navigateTo(path: string): void {
    this.closePanel();
    this.router.navigate([path]);
  }

  openTripPlanner(): void {
    this.router.navigate(['/map'], { queryParams: { plan: 1 } });
  }

  navigateToProfileTab(tab: 'schedule' | 'rsvps' | 'saved' | 'listings'): void {
    this.closePanel();
    this.router.navigate(['/profile'], { queryParams: { tab } });
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }


  logout(): void {
    this.closePanel();
    this.authService.logout();
  }
}
