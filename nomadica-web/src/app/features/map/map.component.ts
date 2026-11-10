import { Component, AfterViewInit, OnDestroy, ViewChild, ElementRef, signal, computed, NgZone, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';
import { PlacesService, MapBbox } from '../../core/services/places.service';
import { CategoriesService } from '../../core/services/categories.service';
import { MapService } from '../../core/services/map.service';
import { AuthService } from '../../core/services/auth.service';
import { ProfileService } from '../../core/services/profile.service';
import { CategoryIconsService } from '../../core/services/category-icons.service';
import { EventsService } from '../../core/services/events.service';
import { TripsService } from '../../core/services/trips.service';
import { GeocodingService, GeocodeResult } from '../../core/services/geocoding.service';
import { DirectionsService } from '../../core/services/directions.service';
import { PlaceSummaryDto } from '../../core/models/place.model';
import { EventSummaryDto } from '../../core/models/event.model';
import { ItineraryDay, MAX_TRIP_DAYS, MIN_TRIP_CATEGORIES } from '../../core/models/trip.model';
import { applyFestivalTheme, FESTIVAL_PALETTE } from '../../core/map/festival-map-theme';

type MapMode = 'places' | 'events' | 'both';

// "Gatherings" holds event types (festival, retreat, ceremony…), so it only makes sense when events are on the map
const EVENTS_ONLY_CATEGORY_SLUG = 'events';

// Distinct, colour-blind-friendly hues so each trip day reads as its own leg of the route
const TRIP_DAY_COLORS = ['#00796b', '#e65100', '#5e35b1', '#c2185b', '#1565c0', '#827717', '#6d4c41'];

const SAVE_TYPE_COLORS: Record<string, string> = {
  WantToGo: '#f9a825',
  Visited: '#5e35b1',
  VisitedLoved: '#e53935'
};

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss'],
  standalone: false
})
export class MapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef;

  mapInstance: mapboxgl.Map | null = null;
  mapMode = signal<MapMode>('places');
  placeCategories = computed(() =>
    this.categoriesService.categories().filter(category => category.slug !== EVENTS_ONLY_CATEGORY_SLUG));
  visibleCategories = computed(() =>
    this.mapMode() === 'places' ? this.placeCategories() : this.categoriesService.categories());
  // Spinner only for the very first load — viewport reloads on pan/zoom stay silent (no flicker)
  showInitialSpinner = signal(true);
  private eventMarkers: mapboxgl.Marker[] = [];
  private loadPlacesTimeout: any = null;
  private savedPlaceTypes = new Map<string, string>();
  private lastRenderedPlaces: PlaceSummaryDto[] = [];

  // Trip planner state
  readonly minTripCategories = MIN_TRIP_CATEGORIES;
  readonly maxTripDays = MAX_TRIP_DAYS;
  isPlannerOpen = signal(false);
  isPlanning = signal(false);
  tripActive = signal(false);
  tripArea = signal<GeocodeResult | null>(null);
  tripAreaResults = signal<GeocodeResult[]>([]);
  tripCategoryIds = signal<string[]>([]);
  tripDays = signal(3);
  itinerary = signal<ItineraryDay[]>([]);
  selectedTripDay = signal<number | null>(null);
  tripStopCount = computed(() => this.itinerary().reduce((total, day) => total + day.stops.length, 0));
  canPlanTrip = computed(() =>
    !!this.tripArea() && this.tripCategoryIds().length >= MIN_TRIP_CATEGORIES && !this.isPlanning());
  private planningSubscription: Subscription | null = null;
  private tripStopPoints: GeoJSON.Feature[] = [];
  private tripRouteLines: (GeoJSON.Feature<GeoJSON.LineString> & { properties: { day: number; color: string } })[] = [];
  private areaSearchTerms = new Subject<string>();

  private hoverPopup: mapboxgl.Popup | null = null;
  private focusPopup: mapboxgl.Popup | null = null;
  private pendingFocusId: string | null = null;
  private pendingFly: { latitude: number; longitude: number; bbox?: [number, number, number, number] } | null = null;

  constructor(
    public placesService: PlacesService,
    public categoriesService: CategoriesService,
    private mapService: MapService,
    private authService: AuthService,
    private profileService: ProfileService,
    private categoryIcons: CategoryIconsService,
    private eventsService: EventsService,
    private tripsService: TripsService,
    private geocodingService: GeocodingService,
    private directionsService: DirectionsService,
    private zone: NgZone,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.areaSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(term => {
      if (term.trim().length < 2) {
        this.tripAreaResults.set([]);
        return;
      }
      this.geocodingService.search(term, 5).subscribe(results => this.tripAreaResults.set(results));
    });
  }

  setMapMode(mode: MapMode): void {
    this.mapMode.set(mode);
    if (mode === 'places' && this.dropHiddenCategorySelections()) {
      this.loadPlacesForViewport();
      return;
    }
    this.refreshForMode();
  }

  // Leaving events mode hides Gatherings, so any of its subcategories still selected would filter invisibly
  private dropHiddenCategorySelections(): boolean {
    const visibleIds = new Set(this.placeCategories().flatMap(parent => (parent.children || []).map(child => child.id)));
    const selected = this.placesService.selectedCategories();
    const stillVisible = selected.filter(id => visibleIds.has(id));
    if (stillVisible.length === selected.length) return false;

    this.placesService.selectedCategories.set(stillVisible);
    return true;
  }

  private refreshForMode(): void {
    const mode = this.mapMode();

    if (mode === 'events') {
      this.setPlacesData([]);
    } else {
      this.renderPlaces(this.lastRenderedPlaces);
    }

    if (mode === 'places') {
      this.clearEventMarkers();
    } else {
      this.loadEventsForViewport();
    }
  }

  ngAfterViewInit(): void {
    this.loadCategories();
    this.loadSavedPlaceIds();
    setTimeout(() => {
      mapboxgl.accessToken = environment.mapboxToken;
      this.initMap();
    }, 100);

    // React to ?plan / ?focus / ?fly* whether arriving fresh or already on the map
    this.route.queryParamMap.subscribe(params => {
      if (params.get('plan') === '1') {
        this.isPlannerOpen.set(true);
        // Clear the flag so the navbar's "Plan a Trip" can reopen the panel on a later click
        this.router.navigate([], { queryParams: { plan: null }, queryParamsHandling: 'merge', replaceUrl: true });
      }

      const focusId = params.get('focus');
      if (focusId) {
        if (this.mapInstance?.isStyleLoaded()) {
          this.focusPlace(focusId);
        } else {
          this.pendingFocusId = focusId;
        }
      }

      const flyLat = params.get('flyLat');
      const flyLng = params.get('flyLng');
      if (flyLat && flyLng) {
        const bboxParam = params.get('flyBbox');
        const bbox = bboxParam
          ? bboxParam.split(',').map(Number) as [number, number, number, number]
          : undefined;
        const target = { latitude: Number(flyLat), longitude: Number(flyLng), bbox };
        if (this.mapInstance?.isStyleLoaded()) {
          this.flyToLocation(target);
        } else {
          this.pendingFly = target;
        }
      }
    });
  }

  private flyToLocation(target: { latitude: number; longitude: number; bbox?: [number, number, number, number] }): void {
    if (!this.mapInstance) return;
    if (target.bbox) {
      this.mapInstance.fitBounds(
        [[target.bbox[0], target.bbox[1]], [target.bbox[2], target.bbox[3]]],
        { padding: 40, duration: 1400 });
    } else {
      this.mapInstance.flyTo({ center: [target.longitude, target.latitude], zoom: 11, duration: 1400 });
    }
  }

  focusPlace(placeId: string): void {
    this.placesService.getById(placeId).subscribe({
      next: (response: any) => {
        const place = response?.place || response;
        if (!place || !this.mapInstance) return;

        this.mapInstance.flyTo({ center: [place.longitude, place.latitude], zoom: 13, duration: 1400 });
        this.focusPopup
          ?.setLngLat([place.longitude, place.latitude])
          .setHTML(this.buildPlacePopupHtml(
            place.name, place.city, place.country,
            this.categoryIcons.getNameForCategoryId(place.categoryId || place.categoryIds?.[0]),
            place.description))
          .addTo(this.mapInstance);
      }
    });
  }

  private buildPlacePopupHtml(
    name: string,
    city?: string,
    country?: string,
    categoryName?: string | null,
    description?: string | null): string {
    const location = [city, country].filter(Boolean).map(part => this.escapeHtml(part!)).join(', ');
    const blurb = this.truncate(description, 120);
    return `<div class="map-popup">
        <strong class="map-popup-title">${this.escapeHtml(name)}</strong>
        ${location ? `<div class="map-popup-loc"><span class="material-icons">place</span>${location}</div>` : ''}
        ${categoryName ? `<div class="map-popup-cat">${this.escapeHtml(categoryName)}</div>` : ''}
        ${blurb ? `<div class="map-popup-desc">${this.escapeHtml(blurb)}</div>` : ''}
      </div>`;
  }

  private truncate(text: string | null | undefined, max: number): string | null {
    if (!text) return null;
    const trimmed = text.trim();
    if (trimmed.length <= max) return trimmed;
    return trimmed.slice(0, max).trimEnd() + '…';
  }

  private placeSubcategory(place: PlaceSummaryDto): string | null {
    return this.categoryIcons.getNameForCategoryId(place.categoryId || place.categoryIds?.[0]);
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  // ── Trip planner ─────────────────────────────────────────────────────────
  togglePlanner(): void {
    if (this.isPlannerOpen()) this.cancelPlanning();
    this.isPlannerOpen.set(!this.isPlannerOpen());
  }

  onPlannerAreaSearch(term: string): void {
    this.areaSearchTerms.next(term);
  }

  selectPlannerArea(result: GeocodeResult, areaInput: HTMLInputElement): void {
    this.tripArea.set(result);
    this.tripAreaResults.set([]);
    areaInput.value = result.fullName;
  }

  onPlannerCategoriesChange(categoryIds: string[]): void {
    this.tripCategoryIds.set(categoryIds);
  }

  changeTripDays(delta: number): void {
    this.tripDays.set(Math.min(MAX_TRIP_DAYS, Math.max(1, this.tripDays() + delta)));
  }

  runTripPlan(): void {
    const area = this.tripArea();
    if (!area || !this.mapInstance || !this.canPlanTrip()) return;

    this.isPlanning.set(true);

    // A bare country geocode has no parent country/city in context → search the whole country
    const isCountryResult = !area.city && !area.country;
    this.planningSubscription = this.tripsService.getItinerary({
      country: isCountryResult ? area.name : undefined,
      latitude: area.latitude,
      longitude: area.longitude,
      radiusKm: isCountryResult ? undefined : 300,
      categoryIds: this.tripCategoryIds(),
      days: this.tripDays()
    }).subscribe({
      next: (itinerary) => {
        this.planningSubscription = null;
        this.isPlanning.set(false);
        this.isPlannerOpen.set(false);
        this.showItinerary(itinerary.days, area);
      },
      error: () => {
        this.planningSubscription = null;
        this.isPlanning.set(false);
      }
    });
  }

  cancelPlanning(): void {
    this.planningSubscription?.unsubscribe();
    this.planningSubscription = null;
    this.isPlanning.set(false);
  }

  clearTrip(): void {
    this.cancelPlanning();
    this.tripActive.set(false);
    this.itinerary.set([]);
    this.selectedTripDay.set(null);
    this.hoverPopup?.remove();
    this.tripStopPoints = [];
    this.tripRouteLines = [];
    this.setTripData([], []);
    // Viewport reloads pause while a trip is shown, so the cached places may be stale by now
    this.loadPlacesForViewport();
  }

  backToPlanner(): void {
    this.clearTrip();
    this.isPlannerOpen.set(true);
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.tripActive()) {
      this.clearTrip();
    } else if (this.isPlannerOpen()) {
      this.togglePlanner();
    }
  }

  dayColor(dayNumber: number): string {
    return TRIP_DAY_COLORS[(dayNumber - 1) % TRIP_DAY_COLORS.length];
  }

  focusTripDay(day: ItineraryDay): void {
    if (!this.mapInstance || day.stops.length === 0) return;

    this.selectedTripDay.set(day.dayNumber);
    this.fitToStops(day.stops);
    if (day.stops.length < 2) return;

    // Road geometry is fetched per day on demand, so planning itself makes no routing calls
    const coordinates = day.stops.map(stop => [stop.longitude, stop.latitude] as [number, number]);
    this.directionsService.getRoute(coordinates).subscribe(roadCoordinates => {
      if (!roadCoordinates || roadCoordinates.length < 2) return;
      this.tripRouteLines = this.tripRouteLines.map(line =>
        line.properties.day === day.dayNumber
          ? { ...line, geometry: { type: 'LineString' as const, coordinates: roadCoordinates } }
          : line);
      this.setTripData(this.tripStopPoints, this.tripRouteLines);
    });
  }

  focusTripStop(stop: PlaceSummaryDto, order: number): void {
    if (!this.mapInstance || !this.hoverPopup) return;
    this.mapInstance.flyTo({ center: [stop.longitude, stop.latitude], zoom: 13, duration: 900 });
    this.hoverPopup
      .setLngLat([stop.longitude, stop.latitude])
      .setHTML(this.buildPlacePopupHtml(`${order}. ${stop.name}`, stop.city, stop.country, this.placeSubcategory(stop)))
      .addTo(this.mapInstance);
  }

  private showItinerary(days: ItineraryDay[], area: GeocodeResult): void {
    if (!this.mapInstance) return;

    this.tripActive.set(true);
    this.itinerary.set(days);
    this.selectedTripDay.set(null);
    this.setPlacesData([]);
    this.clearEventMarkers();

    let order = 0;
    this.tripStopPoints = days.flatMap(day => day.stops.map(stop => ({
      type: 'Feature' as const,
      geometry: { type: 'Point' as const, coordinates: [stop.longitude, stop.latitude] },
      properties: {
        id: stop.id,
        order: ++order,
        color: this.dayColor(day.dayNumber),
        name: stop.name,
        city: stop.city ?? '',
        country: stop.country ?? '',
        category: this.placeSubcategory(stop) ?? ''
      }
    })));

    this.tripRouteLines = days
      .filter(day => day.stops.length >= 2)
      .map(day => ({
        type: 'Feature' as const,
        geometry: {
          type: 'LineString' as const,
          coordinates: day.stops.map(stop => [stop.longitude, stop.latitude])
        },
        properties: { day: day.dayNumber, color: this.dayColor(day.dayNumber) }
      }));

    this.setTripData(this.tripStopPoints, this.tripRouteLines);

    const allStops = days.flatMap(day => day.stops);
    if (allStops.length > 0) {
      this.fitToStops(allStops);
    } else {
      this.flyToArea(area);
    }
  }

  private flyToArea(area: GeocodeResult): void {
    if (!this.mapInstance) return;
    if (area.bbox) {
      this.mapInstance.fitBounds(
        [[area.bbox[0], area.bbox[1]], [area.bbox[2], area.bbox[3]]],
        { padding: 60, duration: 1200 });
    } else {
      this.mapInstance.flyTo({ center: [area.longitude, area.latitude], zoom: 9, duration: 1200 });
    }
  }

  private fitToStops(stops: PlaceSummaryDto[]): void {
    if (!this.mapInstance) return;
    if (stops.length === 1) {
      this.mapInstance.flyTo({ center: [stops[0].longitude, stops[0].latitude], zoom: 12, duration: 1000 });
      return;
    }
    const bounds = new mapboxgl.LngLatBounds();
    stops.forEach(stop => bounds.extend([stop.longitude, stop.latitude]));
    this.mapInstance.fitBounds(bounds, { padding: { top: 120, bottom: 60, left: 60, right: 400 }, duration: 1200, maxZoom: 13 });
  }

  private setTripData(stopPoints: GeoJSON.Feature[], routeLines: GeoJSON.Feature[]): void {
    if (!this.mapInstance) return;
    // The style reports "not loaded" mid-animation; retry once the map settles, using the latest trip state
    if (!this.mapInstance.isStyleLoaded()) {
      this.mapInstance.once('idle', () => this.setTripData(this.tripStopPoints, this.tripRouteLines));
      return;
    }
    this.ensureTripLayers();
    (this.mapInstance.getSource('trip-route') as mapboxgl.GeoJSONSource)
      .setData({ type: 'FeatureCollection', features: routeLines });
    (this.mapInstance.getSource('trip-stops') as mapboxgl.GeoJSONSource)
      .setData({ type: 'FeatureCollection', features: stopPoints });
  }

  // Stops are WebGL layers (not DOM markers) so they move in lockstep with the map while zooming
  private ensureTripLayers(): void {
    const map = this.mapInstance!;
    if (map.getSource('trip-stops')) return;

    const emptyCollection: GeoJSON.FeatureCollection = { type: 'FeatureCollection', features: [] };
    map.addSource('trip-route', { type: 'geojson', data: emptyCollection });
    map.addSource('trip-stops', { type: 'geojson', data: emptyCollection });

    map.addLayer({
      id: 'trip-route',
      type: 'line',
      source: 'trip-route',
      layout: { 'line-join': 'round', 'line-cap': 'round' },
      paint: { 'line-color': ['get', 'color'], 'line-width': 4, 'line-opacity': 0.8 }
    });

    map.addLayer({
      id: 'trip-stop-circles',
      type: 'circle',
      source: 'trip-stops',
      paint: {
        'circle-color': ['get', 'color'],
        'circle-radius': 12,
        'circle-stroke-width': 2,
        'circle-stroke-color': '#ffffff'
      }
    });

    map.addLayer({
      id: 'trip-stop-labels',
      type: 'symbol',
      source: 'trip-stops',
      layout: {
        'text-field': ['to-string', ['get', 'order']],
        'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
        'text-size': 12,
        'text-allow-overlap': true,
        'text-ignore-placement': true
      },
      paint: { 'text-color': '#ffffff' }
    });

    map.on('click', 'trip-stop-circles', (event) => {
      const placeId = event.features?.[0]?.properties?.['id'];
      if (placeId) this.zone.run(() => this.router.navigate(['/places', placeId]));
    });
    map.on('mouseenter', 'trip-stop-circles', (event) => {
      map.getCanvas().style.cursor = 'pointer';
      const feature = event.features?.[0];
      if (!feature || !this.hoverPopup) return;
      const props = feature.properties || {};
      this.hoverPopup
        .setLngLat((feature.geometry as GeoJSON.Point).coordinates as [number, number])
        .setHTML(this.buildPlacePopupHtml(`${props['order']}. ${props['name']}`, props['city'], props['country'], props['category']))
        .addTo(map);
    });
    map.on('mouseleave', 'trip-stop-circles', () => {
      map.getCanvas().style.cursor = '';
      this.hoverPopup?.remove();
    });
  }

  private loadSavedPlaceIds(): void {
    if (!this.authService.isAuthenticated()) return;

    this.profileService.getSavedItems('Place').subscribe({
      next: (response) => {
        this.savedPlaceTypes = new Map(
          (response.items || []).map(item => [item.targetId, item.saveType])
        );
        if (this.lastRenderedPlaces.length > 0) {
          this.renderPlaces(this.lastRenderedPlaces);
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.loadPlacesTimeout) {
      clearTimeout(this.loadPlacesTimeout);
    }
    if (this.mapInstance) {
      this.mapInstance.remove();
    }
  }

  private initMap(): void {
    this.mapInstance = new mapboxgl.Map({
      container: 'map',
      style: 'mapbox://styles/mapbox/light-v11',
      projection: 'mercator',
      zoom: 2.5,
      minZoom: 1.5,
      center: [20, 15],
      renderWorldCopies: false
    });

    // Enable all interactions
    this.mapInstance.dragPan.enable();
    this.mapInstance.scrollZoom.enable();
    this.mapInstance.doubleClickZoom.enable();
    this.mapInstance.boxZoom.enable();
    this.mapInstance.dragRotate.enable();

    // Bottom-right keeps the controls clear of the planner panel / Plan-a-Trip button
    this.mapInstance.addControl(new mapboxgl.NavigationControl(), 'bottom-right');
    this.mapInstance.addControl(new mapboxgl.GeolocateControl({
      positionOptions: { enableHighAccuracy: true },
      trackUserLocation: true
    }), 'bottom-right');

    this.hoverPopup = new mapboxgl.Popup({
      closeButton: false,
      closeOnClick: false,
      offset: 22,
      className: 'nomadica-popup'
    });
    this.focusPopup = new mapboxgl.Popup({ offset: 22, className: 'nomadica-popup' });

    this.mapInstance.on('load', () => {
      applyFestivalTheme(this.mapInstance!);
      this.registerCategoryIcons(this.mapInstance!);
      this.loadPlacesForViewport();
      if (this.pendingFocusId) {
        this.focusPlace(this.pendingFocusId);
        this.pendingFocusId = null;
      }
      if (this.pendingFly) {
        this.flyToLocation(this.pendingFly);
        this.pendingFly = null;
      }
    });

    // Reload listings for the new viewport as the user pans/zooms (debounced)
    this.mapInstance.on('moveend', () => {
      if (this.tripActive()) return;
      if (this.loadPlacesTimeout) clearTimeout(this.loadPlacesTimeout);
      this.loadPlacesTimeout = setTimeout(() => this.loadPlacesForViewport(), 400);
    });
  }

  private getCurrentBbox(): MapBbox | null {
    if (!this.mapInstance) return null;

    const bounds = this.mapInstance.getBounds();
    if (!bounds) return null;

    // Clamp to valid ranges — with renderWorldCopies off, wide viewports report bounds beyond ±180
    const bbox: MapBbox = {
      northEastLat: Math.min(bounds.getNorth(), 90),
      northEastLng: Math.min(bounds.getEast(), 180),
      southWestLat: Math.max(bounds.getSouth(), -90),
      southWestLng: Math.max(bounds.getWest(), -180)
    };

    return this.isValidBbox(bbox) ? bbox : null;
  }

  private loadPlacesForViewport(): void {
    const bbox = this.getCurrentBbox();
    if (!bbox) return;

    const categoryIds = this.placesService.selectedCategories();
    this.placesService.getByViewport(bbox, categoryIds.length > 0 ? categoryIds : undefined)
      .subscribe({
        next: (places) => this.refreshMarkers(places),
        error: () => console.error('Failed to load places')
      });

    if (this.mapMode() !== 'places') {
      this.loadEventsForViewport();
    }
  }

  private loadEventsForViewport(): void {
    const bbox = this.getCurrentBbox();
    if (!bbox) return;

    this.eventsService.getByViewport(bbox).subscribe({
      next: (response) => this.renderEvents(response?.items || []),
      error: () => console.error('Failed to load events')
    });
  }

  private renderEvents(events: EventSummaryDto[]): void {
    if (!this.mapInstance || this.mapMode() === 'places') return;

    this.clearEventMarkers();

    for (const eventItem of events) {
      if (eventItem.latitude == null || eventItem.longitude == null) continue;

      const marker = new mapboxgl.Marker({ element: this.buildEventPinElement(eventItem) })
        .setLngLat([eventItem.longitude, eventItem.latitude])
        .addTo(this.mapInstance);
      this.eventMarkers.push(marker);
    }
  }

  private buildEventPinElement(eventItem: EventSummaryDto): HTMLElement {
    const pin = document.createElement('div');
    pin.className = 'map-pin map-pin--event';
    pin.title = eventItem.title;

    const iconElement = document.createElement('span');
    iconElement.className = 'material-icons';
    iconElement.textContent = 'celebration';
    pin.appendChild(iconElement);

    pin.addEventListener('click', (clickEvent) => {
      clickEvent.stopPropagation();
      this.router.navigate(['/events', eventItem.id]);
    });

    if (eventItem.latitude != null && eventItem.longitude != null) {
      this.attachHoverPopup(pin, eventItem.longitude, eventItem.latitude,
        this.buildPlacePopupHtml(eventItem.title, undefined, undefined,
          this.categoryIcons.getNameForCategoryId(eventItem.categoryIds?.[0]) || 'Event',
          eventItem.description));
    }

    return pin;
  }

  private clearEventMarkers(): void {
    this.eventMarkers.forEach(marker => marker.remove());
    this.eventMarkers = [];
  }

  private isValidBbox(bbox: MapBbox): boolean {
    return (
      bbox.northEastLat >= -90 && bbox.northEastLat <= 90 &&
      bbox.southWestLat >= -90 && bbox.southWestLat <= 90 &&
      bbox.northEastLng >= -180 && bbox.northEastLng <= 180 &&
      bbox.southWestLng >= -180 && bbox.southWestLng <= 180 &&
      bbox.northEastLat > bbox.southWestLat &&
      bbox.northEastLng > bbox.southWestLng
    );
  }

  private refreshMarkers(response: any): void {
    let places: PlaceSummaryDto[] = [];
    if (Array.isArray(response)) {
      places = response;
    } else if (response?.items && Array.isArray(response.items)) {
      places = response.items;
    } else if (response?.data && Array.isArray(response.data)) {
      places = response.data;
    } else if (response?.places && Array.isArray(response.places)) {
      places = response.places;
    }

    this.lastRenderedPlaces = places;
    this.renderPlaces(places);
    this.showInitialSpinner.set(false);
  }

  private renderPlaces(places: PlaceSummaryDto[]): void {
    if (!this.mapInstance) return;
    if (this.tripActive()) return;

    if (this.mapMode() === 'events') {
      this.setPlacesData([]);
      return;
    }

    const validPlaces = places.filter(place =>
      place.latitude >= -90 && place.latitude <= 90 &&
      place.longitude >= -180 && place.longitude <= 180
    );

    this.setPlacesData(validPlaces);
  }

  // Places render as a clustered GeoJSON layer so the map scales to thousands of pins.
  private setPlacesData(places: PlaceSummaryDto[]): void {
    if (!this.mapInstance || !this.mapInstance.isStyleLoaded()) return;

    this.ensurePlacesLayers();

    const features = places.map(place => {
      const saveType = this.savedPlaceTypes.get(place.id) ?? 'None';
      return {
        type: 'Feature' as const,
        geometry: { type: 'Point' as const, coordinates: [place.longitude, place.latitude] },
        properties: {
          id: place.id,
          name: place.name,
          city: place.city ?? '',
          country: place.country ?? '',
          category: this.placeSubcategory(place) ?? '',
          // Multi-category places pin under their primary (first) category
          icon: this.categoryIcons.getEmojiForCategoryId(place.categoryId || place.categoryIds?.[0]),
          pinColor: this.categoryIcons.getPinColorForCategoryId(place.categoryId || place.categoryIds?.[0]),
          saveType
        }
      };
    });

    const source = this.mapInstance.getSource('places-src') as mapboxgl.GeoJSONSource | undefined;
    source?.setData({ type: 'FeatureCollection', features });
  }

  private ensurePlacesLayers(): void {
    const map = this.mapInstance!;
    if (map.getSource('places-src')) return;

    map.addSource('places-src', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: [] },
      cluster: true,
      // Larger grouping radius => fewer, more meaningful bubbles (less clutter as data grows worldwide)
      clusterRadius: 70,
      clusterMaxZoom: 14
    });

    map.addLayer({
      id: 'place-clusters',
      type: 'circle',
      source: 'places-src',
      filter: ['has', 'point_count'],
      paint: {
        'circle-color': ['step', ['get', 'point_count'], FESTIVAL_PALETTE.turquoise, 25, '#26A69A', 100, FESTIVAL_PALETTE.deepTeal],
        // Small, capped radii so even a 1000-count cluster stays compact and doesn't blanket the map
        'circle-radius': ['step', ['get', 'point_count'], 13, 25, 16, 100, 19],
        'circle-opacity': 0.92,
        'circle-stroke-width': 3,
        'circle-stroke-color': FESTIVAL_PALETTE.cream
      }
    });

    map.addLayer({
      id: 'place-cluster-count',
      type: 'symbol',
      source: 'places-src',
      filter: ['has', 'point_count'],
      layout: {
        'text-field': ['get', 'point_count_abbreviated'],
        'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
        'text-size': 12
      },
      paint: { 'text-color': FESTIVAL_PALETTE.cream }
    });

    map.addLayer({
      id: 'place-unclustered',
      type: 'circle',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      paint: {
        // Festival-style round pins: filled with the category colour, cream ring like the style board
        'circle-color': ['get', 'pinColor'],
        // Small dots when zoomed out so pins don't smother each other, growing into round badges that hold the emoji
        'circle-radius': [
          'interpolate', ['linear'], ['zoom'],
          4, 4,
          10, 7,
          12, 12,
          16, 15
        ],
        // Saved state (Want-to-go / Been-there / Loved) moves to a thicker ring so the category colour stays visible
        'circle-stroke-color': ['match', ['get', 'saveType'],
          'WantToGo', SAVE_TYPE_COLORS['WantToGo'],
          'Visited', SAVE_TYPE_COLORS['Visited'],
          'VisitedLoved', SAVE_TYPE_COLORS['VisitedLoved'],
          FESTIVAL_PALETTE.cream],
        'circle-stroke-width': ['case', ['==', ['get', 'saveType'], 'None'], 2, 3.5]
      }
    });

    // Category emoji sits over the coloured dot once zoomed in enough to be legible.
    // The dot alone shows at low zoom so pins stay tiny and don't smother each other.
    map.addLayer({
      id: 'place-icons',
      type: 'symbol',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      minzoom: 11,
      layout: {
        'icon-image': ['get', 'icon'],
        // Sized to sit inside the round pin (emoji images are 32 CSS px at size 1)
        'icon-size': ['interpolate', ['linear'], ['zoom'], 11, 0.5, 12, 0.62, 16, 0.78],
        'icon-allow-overlap': true,
        'icon-ignore-placement': true
      }
    });

    // Click a cluster to zoom into it
    map.on('click', 'place-clusters', (event) => {
      const clustered = map.queryRenderedFeatures(event.point, { layers: ['place-clusters'] });
      const clusterId = clustered[0]?.properties?.['cluster_id'];
      if (clusterId == null) return;
      const source = map.getSource('places-src') as mapboxgl.GeoJSONSource;
      source.getClusterExpansionZoom(clusterId, (error, zoom) => {
        if (error || zoom == null) return;
        const geometry = clustered[0].geometry as GeoJSON.Point;
        map.easeTo({ center: geometry.coordinates as [number, number], zoom });
      });
    });

    // Open a place (inside Angular zone so routing/CD fire)
    const openPlace = (event: mapboxgl.MapLayerMouseEvent) => {
      const placeId = event.features?.[0]?.properties?.['id'];
      if (placeId) this.zone.run(() => this.router.navigate(['/places', placeId]));
    };

    const showPointPopup = (event: mapboxgl.MapLayerMouseEvent) => {
      const feature = event.features?.[0];
      if (!feature || !this.hoverPopup) return;
      const props = feature.properties || {};
      const geometry = feature.geometry as GeoJSON.Point;
      this.hoverPopup
        .setLngLat(geometry.coordinates as [number, number])
        .setHTML(this.buildPlacePopupHtml(props['name'], props['city'], props['country'], props['category']))
        .addTo(map);
    };

    // Both the dot and the emoji on top are clickable/hoverable
    for (const layerId of ['place-unclustered', 'place-icons']) {
      map.on('click', layerId, openPlace);
      map.on('mouseenter', layerId, (event) => {
        map.getCanvas().style.cursor = 'pointer';
        showPointPopup(event);
      });
      map.on('mousemove', layerId, showPointPopup);
      map.on('mouseleave', layerId, () => {
        map.getCanvas().style.cursor = '';
        this.hoverPopup?.remove();
      });
    }
    map.on('mouseenter', 'place-clusters', () => { map.getCanvas().style.cursor = 'pointer'; });
    map.on('mouseleave', 'place-clusters', () => { map.getCanvas().style.cursor = ''; });
  }

  // Render each category emoji to a small image Mapbox can use in the symbol layer.
  // Runs once per map load; skips any image already registered.
  private registerCategoryIcons(map: mapboxgl.Map): void {
    const pixelSize = 64;
    for (const emoji of this.categoryIcons.getAllPinEmojis()) {
      if (map.hasImage(emoji)) continue;
      const canvas = document.createElement('canvas');
      canvas.width = pixelSize;
      canvas.height = pixelSize;
      const context = canvas.getContext('2d');
      if (!context) continue;
      context.font = `${Math.floor(pixelSize * 0.72)}px "Apple Color Emoji","Segoe UI Emoji","Noto Color Emoji",sans-serif`;
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      context.fillText(emoji, pixelSize / 2, pixelSize / 2 + pixelSize * 0.04);
      const image = context.getImageData(0, 0, pixelSize, pixelSize);
      if (!map.hasImage(emoji)) map.addImage(emoji, image, { pixelRatio: 2 });
    }
  }

  private attachHoverPopup(element: HTMLElement, lng: number, lat: number, html: string): void {
    element.addEventListener('mouseenter', () => {
      if (!this.mapInstance || !this.hoverPopup) return;
      this.hoverPopup.setLngLat([lng, lat]).setHTML(html).addTo(this.mapInstance);
    });
    element.addEventListener('mouseleave', () => this.hoverPopup?.remove());
  }

  onCategoryChange(categoryIds: string[]): void {
    this.placesService.selectedCategories.set(categoryIds);
    this.loadPlacesForViewport();
  }

  private loadCategories(): void {
    this.categoriesService.loadCategories().subscribe({
      next: () => {
        this.categoryIcons.buildIndex(this.categoriesService.categories());
        if (this.lastRenderedPlaces.length > 0) {
          this.renderPlaces(this.lastRenderedPlaces);
        }
      },
      error: () => this.categoriesService.categories.set([])
    });
  }
}

