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
import { PlaceDensityCell, PlaceSummaryDto } from '../../core/models/place.model';
import { EventSummaryDto } from '../../core/models/event.model';
import { ItineraryDay, MAX_TRIP_DAYS, MIN_TRIP_CATEGORIES } from '../../core/models/trip.model';
import { applyFestivalTheme, FESTIVAL_PALETTE } from '../../core/map/festival-map-theme';

type MapMode = 'places' | 'events' | 'both';

// Below this zoom (roughly a whole country in view) the server sends per-cell counts instead of individual places,
// so the totals cover every place rather than one 1000-place page
const DENSITY_MAX_ZOOM = 9;

// Matches the SCSS breakpoint where the itinerary becomes a bottom sheet capped at 45vh
const MOBILE_BREAKPOINT_PX = 760;
const MOBILE_SHEET_HEIGHT_RATIO = 0.45;

const BEAR_WALK_FRAMES =[1, 2, 3, 4].map(frame => `brand/bears/walk-${frame}.png`);
const BEAR_ARRIVED_IMAGE = 'brand/bears/waving.png';
const BEAR_WALK_FRAME_MS = 140;
const BEAR_WALK_MIN_MS = 6000;
const BEAR_WALK_MAX_MS = 14000;

// Mapbox expression: true for pins whose category has a mascot pose (see CategoryIconsService)
const HAS_BEAR_POSE: mapboxgl.ExpressionSpecification = ['!=', ['get', 'bear'], ''];

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
  private lastDensityCells: PlaceDensityCell[] = [];
  private isDensityView = false;
  private isPlaceRenderPending = false;

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
  private walkingBear: mapboxgl.Marker | null = null;
  private walkAnimationFrame: number | null = null;
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

    this.renderPlaceLayers();

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
    this.stopBearWalk();
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
    this.stopBearWalk();
    if (day.stops.length < 2) return;

    // Road geometry is fetched per day on demand, so planning itself makes no routing calls
    const coordinates = day.stops.map(stop => [stop.longitude, stop.latitude] as [number, number]);
    this.directionsService.getRoute(coordinates).subscribe(roadCoordinates => {
      const hasRoad = !!roadCoordinates && roadCoordinates.length > 1;
      if (hasRoad) {
        this.tripRouteLines = this.tripRouteLines.map(line =>
          line.properties.day === day.dayNumber
            ? { ...line, geometry: { type: 'LineString' as const, coordinates: roadCoordinates! } }
            : line);
        this.setTripData(this.tripStopPoints, this.tripRouteLines);
      }
      // Only walk if the user is still looking at this day
      if (this.selectedTripDay() === day.dayNumber) {
        this.startBearWalk(hasRoad ? roadCoordinates! : coordinates);
      }
    });
  }

  // The mascot walks the day's route stop to stop: one DOM marker, frames swapped while it moves
  private startBearWalk(path: [number, number][]): void {
    if (!this.mapInstance || path.length < 2) return;
    this.stopBearWalk();

    const element = document.createElement('div');
    element.className = 'walking-bear';
    const sprite = document.createElement('img');
    sprite.src = BEAR_WALK_FRAMES[0];
    sprite.alt = '';
    element.appendChild(sprite);
    this.walkingBear = new mapboxgl.Marker({ element, anchor: 'bottom' }).setLngLat(path[0]).addTo(this.mapInstance);

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion) {
      this.walkingBear.setLngLat(path[path.length - 1]);
      return;
    }

    // Distances in degrees with longitude scaled by latitude: plenty for pacing an animation
    const segmentLengths = path.slice(1).map((point, index) => {
      const previous = path[index];
      const longitudeScale = Math.cos((previous[1] * Math.PI) / 180);
      return Math.hypot((point[0] - previous[0]) * longitudeScale, point[1] - previous[1]);
    });
    const totalLength = segmentLengths.reduce((sum, length) => sum + length, 0) || 1;
    const durationMs = Math.min(BEAR_WALK_MAX_MS, Math.max(BEAR_WALK_MIN_MS, path.length * 120));
    const startedAt = performance.now();

    this.zone.runOutsideAngular(() => {
      const step = (now: number) => {
        if (!this.walkingBear) return;
        const progress = Math.min(1, (now - startedAt) / durationMs);
        let remaining = progress * totalLength;
        let segment = 0;
        while (segment < segmentLengths.length - 1 && remaining > segmentLengths[segment]) {
          remaining -= segmentLengths[segment];
          segment++;
        }
        const from = path[segment];
        const to = path[segment + 1];
        const fraction = segmentLengths[segment] ? Math.min(1, remaining / segmentLengths[segment]) : 1;
        this.walkingBear.setLngLat([from[0] + (to[0] - from[0]) * fraction, from[1] + (to[1] - from[1]) * fraction]);

        // Sprites face right; mirror when heading west
        sprite.style.transform = to[0] < from[0] ? 'scaleX(-1)' : '';
        sprite.src = progress < 1
          ? BEAR_WALK_FRAMES[Math.floor((now - startedAt) / BEAR_WALK_FRAME_MS) % BEAR_WALK_FRAMES.length]
          : BEAR_ARRIVED_IMAGE;

        if (progress < 1) this.walkAnimationFrame = requestAnimationFrame(step);
      };
      this.walkAnimationFrame = requestAnimationFrame(step);
    });
  }

  private stopBearWalk(): void {
    if (this.walkAnimationFrame !== null) cancelAnimationFrame(this.walkAnimationFrame);
    this.walkAnimationFrame = null;
    this.walkingBear?.remove();
    this.walkingBear = null;
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
    this.hidePlaceLayers();
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
    this.mapInstance.fitBounds(bounds, { padding: this.tripPanelPadding(), duration: 1200, maxZoom: 13 });
  }

  // Keep stops clear of the itinerary panel: beside it on wide screens, above the bottom sheet on phones.
  // A fixed 400px right margin is wider than a phone screen, which made fitBounds silently do nothing.
  private tripPanelPadding(): mapboxgl.PaddingOptions {
    const canvas = this.mapInstance!.getCanvas();
    if (canvas.clientWidth > MOBILE_BREAKPOINT_PX) {
      return { top: 120, bottom: 60, left: 60, right: 400 };
    }
    const sheetHeight = Math.round(window.innerHeight * MOBILE_SHEET_HEIGHT_RATIO);
    return { top: 140, bottom: Math.min(sheetHeight + 20, canvas.clientHeight / 2), left: 40, right: 40 };
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
        this.renderPlaceLayers();
      }
    });
  }

  ngOnDestroy(): void {
    this.stopBearWalk();
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
      className: 'bear-necessities-popup'
    });
    this.focusPopup = new mapboxgl.Popup({ offset: 22, className: 'bear-necessities-popup' });

    this.mapInstance.on('load', () => {
      applyFestivalTheme(this.mapInstance!);
      this.registerCategoryIcons(this.mapInstance!);
      this.registerBearImages(this.mapInstance!);
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
    if (!bbox || !this.mapInstance) return;

    const selected = this.placesService.selectedCategories();
    const categoryFilter = selected.length > 0 ? selected : undefined;
    const zoom = this.mapInstance.getZoom();

    if (zoom < DENSITY_MAX_ZOOM) {
      this.placesService.getDensity(bbox, zoom, categoryFilter).subscribe({
        next: (density) => {
          this.isDensityView = true;
          this.lastDensityCells = density.cells || [];
          this.renderPlaceLayers();
          this.showInitialSpinner.set(false);
        },
        error: () => console.error('Failed to load place density')
      });
    } else {
      this.placesService.getByViewport(bbox, categoryFilter).subscribe({
        next: (places) => this.refreshMarkers(places),
        error: () => console.error('Failed to load places')
      });
    }

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

    this.isDensityView = false;
    this.lastRenderedPlaces = places;
    this.renderPlaceLayers();
    this.showInitialSpinner.set(false);
  }

  // Single place to decide what the place layers show: density cells (zoomed out), pins (zoomed in) or nothing
  private renderPlaceLayers(): void {
    if (!this.mapInstance) return;
    if (this.tripActive()) return;
    // Mapbox reports the style as not loaded while tiles stream in (e.g. right after a jump);
    // dropping the update then left the map empty, so redraw from the latest state once it settles
    if (!this.mapInstance.isStyleLoaded()) {
      if (!this.isPlaceRenderPending) {
        this.isPlaceRenderPending = true;
        this.mapInstance.once('idle', () => {
          this.isPlaceRenderPending = false;
          this.renderPlaceLayers();
        });
      }
      return;
    }

    const showsPlaces = this.mapMode() !== 'events';
    const validPlaces = this.lastRenderedPlaces.filter(place =>
      place.latitude >= -90 && place.latitude <= 90 &&
      place.longitude >= -180 && place.longitude <= 180
    );

    this.setDensityData(showsPlaces && this.isDensityView ? this.lastDensityCells : []);
    this.setPlacesData(showsPlaces && !this.isDensityView ? validPlaces : []);
  }

  private hidePlaceLayers(): void {
    if (!this.mapInstance) return;
    if (!this.mapInstance.isStyleLoaded()) {
      this.mapInstance.once('idle', () => this.hidePlaceLayers());
      return;
    }
    this.setPlacesData([]);
    this.setDensityData([]);
  }

  // Callers check readiness once up front: adding one source flips isStyleLoaded() to false, which would
  // otherwise make the next layer update in the same pass bail out
  private setDensityData(cells: PlaceDensityCell[]): void {
    if (!this.mapInstance) return;
    this.ensureDensityLayers();

    const features = cells.map(cell => ({
      type: 'Feature' as const,
      geometry: { type: 'Point' as const, coordinates: [cell.longitude, cell.latitude] },
      // Bounds go in as scalars: Mapbox serialises array properties to strings on query
      properties: {
        count: cell.count,
        label: this.abbreviateCount(cell.count),
        west: cell.bounds[0],
        south: cell.bounds[1],
        east: cell.bounds[2],
        north: cell.bounds[3]
      }
    }));
    (this.mapInstance.getSource('density-src') as mapboxgl.GeoJSONSource)
      .setData({ type: 'FeatureCollection', features });
  }

  private abbreviateCount(count: number): string {
    if (count < 1000) return String(count);
    return `${(count / 1000).toFixed(count < 10000 ? 1 : 0).replace(/\.0$/, '')}k`;
  }

  // Density cells look like clusters but carry exact server-side counts for the whole viewport
  private ensureDensityLayers(): void {
    const map = this.mapInstance!;
    if (map.getSource('density-src')) return;

    map.addSource('density-src', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });

    map.addLayer({
      id: 'density-cells',
      type: 'circle',
      source: 'density-src',
      paint: {
        'circle-color': ['step', ['get', 'count'], FESTIVAL_PALETTE.turquoise, 25, '#26A69A', 100, FESTIVAL_PALETTE.deepTeal],
        'circle-radius': ['step', ['get', 'count'], 13, 25, 16, 100, 19, 1000, 23],
        'circle-opacity': 0.92,
        'circle-stroke-width': 3,
        'circle-stroke-color': FESTIVAL_PALETTE.cream
      }
    });

    map.addLayer({
      id: 'density-count',
      type: 'symbol',
      source: 'density-src',
      layout: {
        'text-field': ['get', 'label'],
        'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
        'text-size': 12,
        'text-allow-overlap': true
      },
      paint: { 'text-color': FESTIVAL_PALETTE.cream }
    });

    // Zoom straight to where the cell's places actually are (like Mapbox's cluster expansion), so a
    // country-sized cell opens up in one click instead of creeping in a couple of zoom levels at a time
    map.on('click', 'density-cells', (event) => {
      const props = event.features?.[0]?.properties;
      if (!props) return;
      const spansArea = props['east'] - props['west'] > 0.0005 || props['north'] - props['south'] > 0.0005;
      if (!spansArea) {
        map.easeTo({ center: [props['west'], props['south']], zoom: DENSITY_MAX_ZOOM + 4 });
        return;
      }
      map.fitBounds([[props['west'], props['south']], [props['east'], props['north']]],
        { padding: 60, maxZoom: DENSITY_MAX_ZOOM + 4, duration: 900 });
    });
    map.on('mouseenter', 'density-cells', () => { map.getCanvas().style.cursor = 'pointer'; });
    map.on('mouseleave', 'density-cells', () => { map.getCanvas().style.cursor = ''; });
  }

  // Places render as a clustered GeoJSON layer so the map scales to thousands of pins.
  private setPlacesData(places: PlaceSummaryDto[]): void {
    if (!this.mapInstance) return;

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
          bear: this.categoryIcons.getBearImageIdForCategoryId(place.categoryId || place.categoryIds?.[0]),
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
        // Small dots when zoomed out; zoomed in, a mascot pin shrinks its dot to a "ground spot" the bear stands on,
        // while emoji-only categories grow into round badges that hold the emoji
        'circle-radius': [
          'interpolate', ['linear'], ['zoom'],
          4, 4,
          10, 7,
          11.9, 9,
          12, ['case', HAS_BEAR_POSE, 5, 12],
          16, ['case', HAS_BEAR_POSE, 6, 15]
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
      minzoom: 12,
      layout: {
        // The bear pose when the category has one (and its image has loaded), otherwise the category emoji
        'icon-image': ['coalesce', ['image', ['get', 'bear']], ['image', ['get', 'icon']]],
        // Bears (48 CSS px tall at size 1) stand on the dot; emoji (32 px) sit inside the round badge
        'icon-size': ['interpolate', ['linear'], ['zoom'],
          12, ['case', HAS_BEAR_POSE, 0.75, 0.62],
          16, ['case', HAS_BEAR_POSE, 1.1, 0.78]],
        'icon-anchor': ['case', HAS_BEAR_POSE, 'bottom', 'center'],
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

  // Pins fall back to emoji until each pose image arrives, then Mapbox redraws them with the bear
  private registerBearImages(map: mapboxgl.Map): void {
    for (const [imageId, url] of this.categoryIcons.getAllBearPinImages()) {
      if (map.hasImage(imageId)) continue;
      map.loadImage(url, (error, image) => {
        if (error || !image || map.hasImage(imageId)) return;
        map.addImage(imageId, image, { pixelRatio: 2 });
      });
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
        this.renderPlaceLayers();
      },
      error: () => this.categoriesService.categories.set([])
    });
  }
}

