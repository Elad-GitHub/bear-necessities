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
import { TripsService } from '../../core/services/trips.service';
import { GeocodingService, GeocodeResult } from '../../core/services/geocoding.service';
import { DirectionsService } from '../../core/services/directions.service';
import { PlaceDensityCell, PlaceSummaryDto } from '../../core/models/place.model';
import { ItineraryDay, MAX_TRIP_DAYS, MIN_TRIP_CATEGORIES } from '../../core/models/trip.model';
import { applyFestivalTheme, FESTIVAL_PALETTE } from '../../core/map/festival-map-theme';
import { sizedPhotoUrl } from '../../core/utils/photo-size';
import { PAW_CLUSTER_PAINT, pawClusterLayout, registerPawClusterImages } from '../../core/map/paw-clusters';
import { measureSegments, PAW_IMAGE_ID, PawPrint, pawPrintCountFor, registerPawTrailImage, samplePawPrints, toPawFeatures } from '../../core/map/paw-trail';
import { loadMapImages } from '../../core/map/map-images';
import { MapViewStateService } from '../../core/map/map-view-state.service';
import { FilterPulse, PulsePoint } from '../../core/map/filter-pulse';

type WhenOption = 'anytime' | 'week' | 'month' | 'custom';

// Below this zoom (roughly a whole country in view) the server sends per-cell counts instead of individual places,
// so the totals cover every place rather than one 1000-place page
const DENSITY_MAX_ZOOM = 9;
// First visit: a world view centred between the Americas and Asia
const DEFAULT_CENTER: [number, number] = [20, 15];
const DEFAULT_ZOOM = 2.5;

// Matches the SCSS breakpoint where the itinerary becomes a bottom sheet capped at 45vh
const MOBILE_BREAKPOINT_PX = 760;
const MOBILE_SHEET_HEIGHT_RATIO = 0.45;

// Mustard from the festival palette: events wear it as a ring so they read differently from cream-ringed places
const EVENT_RING_COLOR = '#FBC02D';
const POPUP_DESCRIPTION_LENGTH = 120;
const ONGOING_EVENT_LABEL = 'Ongoing';
const DAYS_IN_WEEK = 7;
const DAYS_IN_MONTH = 30;
const MS_PER_DAY = 86_400_000;
// The whole map, for counting every match of a filter wherever it is
const WORLD_BBOX: MapBbox = { northEastLat: 85, northEastLng: 180, southWestLat: -85, southWestLng: -180 };
const WORLD_DENSITY_ZOOM = 2;
const SHOW_FOUND_MAX_ZOOM = 13;

/** How many places match the active filter across the whole map, and where they are. */
interface FoundSummary {
  label: string;
  count: number;
  bounds: [number, number, number, number] | null;
}

const BEAR_WALK_FRAMES = [1, 2, 3, 4].map(frame => `brand/bears/walk-${frame}.png`);
const BEAR_ARRIVED_IMAGE = 'brand/bears/waving.png';
const BEAR_WALK_FRAME_MS = 140;
const TRIP_ROUTE_OPACITY = 0.8;
const POPUP_PHOTO_WIDTH_PX = 330;
const TRIP_STOP_CLUSTER_RADIUS_PX = 22;
const TRIP_STOP_CLUSTER_MAX_ZOOM = 16;
// About 15 m: enough to separate stops in one building without misplacing them
const SHARED_SPOT_SPREAD_DEGREES = 0.00014;
const WALKED_ROUTE_OPACITY = 0.25;
const BEAR_WALK_MIN_MS = 6000;
const BEAR_WALK_MAX_MS = 14000;

// Mapbox expression: true for pins whose category has a mascot pose (see CategoryIconsService)
const HAS_BEAR_POSE: mapboxgl.ExpressionSpecification = ['!=', ['get', 'bear'], ''];
const HAS_EVENT: mapboxgl.ExpressionSpecification = ['!=', ['get', 'eventLabel'], ''];

// "Gatherings" holds event types (festival, retreat, ceremony…), so it only makes sense when events are on the map

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
  // "When?" filter: Anytime shows every place; with days chosen, only what's open or happening then
  whenOption = signal<WhenOption>('anytime');
  isDatePickerOpen = signal(false);
  customFrom = signal('');
  customTo = signal('');
  foundSummary = signal<FoundSummary | null>(null);
  private filterPulse: FilterPulse | null = null;
  private foundRequestId = 0;
  // Spinner only for the very first load — viewport reloads on pan/zoom stay silent (no flicker)
  showInitialSpinner = signal(true);
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
    private tripsService: TripsService,
    private geocodingService: GeocodingService,
    private directionsService: DirectionsService,
    private zone: NgZone,
    private route: ActivatedRoute,
    private router: Router,
    private mapViewState: MapViewStateService
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

  setWhen(option: Exclude<WhenOption, 'custom'>): void {
    this.whenOption.set(option);
    this.isDatePickerOpen.set(false);
    const days = option === 'week' ? DAYS_IN_WEEK : option === 'month' ? DAYS_IN_MONTH : 0;
    const today = new Date();
    this.placesService.selectedDates.set(days
      ? { from: this.toIsoDate(today), to: this.toIsoDate(new Date(today.getTime() + (days - 1) * MS_PER_DAY)) }
      : null);
    this.loadPlacesForViewport();
    this.updateFilterHighlight();
  }

  toggleDatePicker(): void {
    this.isDatePickerOpen.set(!this.isDatePickerOpen());
    if (!this.customFrom()) this.customFrom.set(this.toIsoDate(new Date()));
  }

  applyCustomDates(): void {
    const from = this.customFrom();
    const to = this.customTo() || from;
    if (!from) return;
    this.whenOption.set('custom');
    this.isDatePickerOpen.set(false);
    this.placesService.selectedDates.set(from <= to ? { from, to } : { from: to, to: from });
    this.loadPlacesForViewport();
    this.updateFilterHighlight();
  }

  /** Flies the map so every place matching the filter is in view. */
  showFound(): void {
    const bounds = this.foundSummary()?.bounds;
    if (!bounds || !this.mapInstance) return;
    const [west, south, east, north] = bounds;
    if (east - west < 0.001 && north - south < 0.001) {
      this.mapInstance.flyTo({ center: [west, south], zoom: SHOW_FOUND_MAX_ZOOM, duration: 1400 });
      return;
    }
    this.mapInstance.fitBounds([[west, south], [east, north]], { padding: 90, maxZoom: SHOW_FOUND_MAX_ZOOM, duration: 1400 });
  }

  // While a category filter is on, matches pulse on the map and a chip says how many there are in total
  private updateFilterHighlight(): void {
    const selected = this.placesService.selectedCategories();
    this.updatePulses();
    if (selected.length === 0 || this.tripActive()) {
      this.foundSummary.set(null);
      return;
    }

    const label = this.describeSelection(selected);
    const requestId = ++this.foundRequestId;
    this.placesService.getDensity(WORLD_BBOX, WORLD_DENSITY_ZOOM, selected).subscribe({
      next: (density) => {
        if (requestId !== this.foundRequestId) return;
        const cells = density.cells || [];
        const bounds = cells.length === 0 ? null : cells.reduce<[number, number, number, number]>(
          (union, cell) => [
            Math.min(union[0], cell.bounds[0]), Math.min(union[1], cell.bounds[1]),
            Math.max(union[2], cell.bounds[2]), Math.max(union[3], cell.bounds[3])
          ], [...cells[0].bounds]);
        this.foundSummary.set({ label, count: density.totalCount, bounds });
      }
    });
  }

  // Ring whatever the map currently shows for the filter: the paws when zoomed out, the pins when zoomed in
  private updatePulses(): void {
    if (!this.filterPulse) return;
    if (this.placesService.selectedCategories().length === 0 || this.tripActive()) {
      this.filterPulse.hide();
      return;
    }
    const points: PulsePoint[] = this.isDensityView
      ? this.lastDensityCells.map(cell => ({ longitude: cell.longitude, latitude: cell.latitude, isLarge: true }))
      : this.lastRenderedPlaces.map(place => ({ longitude: place.longitude, latitude: place.latitude, isLarge: false }));
    this.filterPulse.show(points);
  }

  // One subcategory reads by name ("Retreat"); a whole category by its parent ("Healing")
  private describeSelection(selectedIds: string[]): string {
    if (selectedIds.length === 1) return this.categoryIcons.getNameForCategoryId(selectedIds[0]) ?? 'Your filter';
    const selected = new Set(selectedIds);
    const wholeParent = this.categoriesService.categories().find(parent => {
      const childIds = (parent.children || []).map(child => child.id);
      return childIds.length === selected.size && childIds.every(id => selected.has(id));
    });
    return wholeParent?.name ?? `${selectedIds.length} categories`;
  }

  customDatesLabel(): string {
    const dates = this.placesService.selectedDates();
    if (this.whenOption() !== 'custom' || !dates) return 'Pick dates';
    const format = (iso: string) => new Date(iso + 'T00:00:00').toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
    return dates.from === dates.to ? format(dates.from) : format(dates.from) + ' – ' + format(dates.to);
  }

  private toIsoDate(date: Date): string {
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 10);
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
            place.description,
            null,
            sizedPhotoUrl(place.photoUrl, POPUP_PHOTO_WIDTH_PX)))
          .addTo(this.mapInstance);
      }
    });
  }

  private buildPlacePopupHtml(
    name: string,
    city?: string,
    country?: string,
    categoryName?: string | null,
    description?: string | null,
    nextEvent?: string | null,
    photoUrl?: string | null): string {
    const location = [city, country].filter(Boolean).map(part => this.escapeHtml(part!)).join(', ');
    const blurb = this.truncate(description, 120);
    return `<div class="map-popup">
        ${photoUrl ? `<img class="map-popup-photo" src="${this.escapeHtml(photoUrl)}" alt="" />` : ''}
        <strong class="map-popup-title">${this.escapeHtml(name)}</strong>
        ${location ? `<div class="map-popup-loc"><span class="material-icons">place</span>${location}</div>` : ''}
        ${categoryName ? `<div class="map-popup-cat">${this.escapeHtml(categoryName)}</div>` : ''}
        ${nextEvent ? `<div class="map-popup-next">Next: ${this.escapeHtml(nextEvent)}</div>` : ''}
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
    this.updateFilterHighlight();
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
      // Start once the camera has settled on the day (print spacing is measured on screen),
      // and only if the user is still looking at this day
      const walk = () => {
        if (this.selectedTripDay() !== day.dayNumber) return;
        this.startBearWalk(hasRoad ? roadCoordinates! : coordinates);
        this.highlightWalkedDay(day.dayNumber);
      };
      if (this.mapInstance?.isMoving()) this.mapInstance.once('moveend', walk);
      else walk();
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

    const segmentLengths = measureSegments(path);
    const map = this.mapInstance;
    const routeLengthPixels = path.slice(1).reduce((sum, point, index) => {
      const from = map.project(path[index]);
      const to = map.project(point);
      return sum + Math.hypot(to.x - from.x, to.y - from.y);
    }, 0);
    const pawPrints = samplePawPrints(path, segmentLengths, pawPrintCountFor(routeLengthPixels));

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion) {
      this.walkingBear.setLngLat(path[path.length - 1]);
      sprite.src = BEAR_ARRIVED_IMAGE;
      this.setBearTrail(pawPrints);
      return;
    }

    const totalLength = segmentLengths.reduce((sum, length) => sum + length, 0) || 1;
    let revealedPrints = 0;
    const durationMs = Math.min(BEAR_WALK_MAX_MS, Math.max(BEAR_WALK_MIN_MS, path.length * 120));
    const startedAt = performance.now();

    this.zone.runOutsideAngular(() => {
      const step = (now: number) => {
        if (!this.walkingBear) return;
        const progress = Math.min(1, (now - startedAt) / durationMs);
        const walked = progress * totalLength;

        // Prints appear just behind the bear; the source only updates when a new print is due
        let printsDue = revealedPrints;
        while (printsDue < pawPrints.length && pawPrints[printsDue].distance <= walked) printsDue++;
        if (printsDue !== revealedPrints) {
          revealedPrints = printsDue;
          this.setBearTrail(pawPrints.slice(0, revealedPrints));
        }

        let remaining = walked;
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
    this.setBearTrail([]);
    this.highlightWalkedDay(null);
  }

  private setBearTrail(prints: PawPrint[]): void {
    const source = this.mapInstance?.getSource('bear-trail') as mapboxgl.GeoJSONSource | undefined;
    source?.setData({ type: 'FeatureCollection', features: toPawFeatures(prints) });
  }

  // The walked day's line fades so its paw prints carry the route; other days keep their solid lines
  private highlightWalkedDay(dayNumber: number | null): void {
    if (!this.mapInstance?.getLayer('trip-route')) return;
    this.mapInstance.setPaintProperty('trip-route', 'line-opacity', dayNumber === null
      ? TRIP_ROUTE_OPACITY
      : ['case', ['==', ['get', 'day'], dayNumber], WALKED_ROUTE_OPACITY, TRIP_ROUTE_OPACITY]);
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

    let order = 0;
    const spread = this.spreadSharedSpots(days.flatMap(day => day.stops));
    this.tripStopPoints = days.flatMap(day => day.stops.map(stop => ({
      type: 'Feature' as const,
      geometry: { type: 'Point' as const, coordinates: spread.get(stop.id) ?? [stop.longitude, stop.latitude] },
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

  // Several stops in one building would sit exactly on top of each other even fully zoomed in,
  // so stops sharing a spot fan out in a small ring around it
  private spreadSharedSpots(stops: PlaceSummaryDto[]): Map<string, [number, number]> {
    const bySpot = new Map<string, PlaceSummaryDto[]>();
    for (const stop of stops) {
      const spot = `${stop.latitude.toFixed(4)},${stop.longitude.toFixed(4)}`;
      bySpot.set(spot, [...(bySpot.get(spot) ?? []), stop]);
    }
    const positions = new Map<string, [number, number]>();
    for (const shared of bySpot.values()) {
      if (shared.length < 2) continue;
      shared.forEach((stop, index) => {
        const angle = (2 * Math.PI * index) / shared.length;
        positions.set(stop.id, [
          stop.longitude + SHARED_SPOT_SPREAD_DEGREES * Math.cos(angle),
          stop.latitude + SHARED_SPOT_SPREAD_DEGREES * Math.sin(angle)
        ]);
      });
    }
    return positions;
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
    // Stops close enough to overlap merge into one circle labelled with their range ("3–5")
    map.addSource('trip-stops', {
      type: 'geojson',
      data: emptyCollection,
      cluster: true,
      clusterRadius: TRIP_STOP_CLUSTER_RADIUS_PX,
      clusterMaxZoom: TRIP_STOP_CLUSTER_MAX_ZOOM,
      clusterProperties: {
        firstOrder: ['min', ['get', 'order']],
        lastOrder: ['max', ['get', 'order']]
      }
    });

    map.addLayer({
      id: 'trip-route',
      type: 'line',
      source: 'trip-route',
      layout: { 'line-join': 'round', 'line-cap': 'round' },
      paint: { 'line-color': ['get', 'color'], 'line-width': 4, 'line-opacity': TRIP_ROUTE_OPACITY }
    });

    registerPawTrailImage(map);
    map.addSource('bear-trail', { type: 'geojson', data: emptyCollection });
    map.addLayer({
      id: 'bear-trail',
      type: 'symbol',
      source: 'bear-trail',
      layout: {
        'icon-image': PAW_IMAGE_ID,
        'icon-size': ['interpolate', ['linear'], ['zoom'], 10, 0.55, 14, 0.75, 17, 0.95],
        // Prints face the walking direction and stay glued to the ground when the map rotates
        'icon-rotate': ['get', 'bearing'],
        'icon-rotation-alignment': 'map',
        // Left and right feet sit either side of the route, like real tracks
        'icon-offset': ['case', ['==', ['get', 'side'], 1], ['literal', [9, 0]], ['literal', [-9, 0]]],
        'icon-allow-overlap': true,
        'icon-ignore-placement': true
      }
    });

    map.addLayer({
      id: 'trip-stop-circles',
      type: 'circle',
      source: 'trip-stops',
      filter: ['!', ['has', 'point_count']],
      paint: {
        'circle-color': ['get', 'color'],
        'circle-radius': 12,
        'circle-stroke-width': 2,
        'circle-stroke-color': '#ffffff'
      }
    });

    map.addLayer({
      id: 'trip-stop-groups',
      type: 'circle',
      source: 'trip-stops',
      filter: ['has', 'point_count'],
      paint: {
        'circle-color': FESTIVAL_PALETTE.darkTeal,
        'circle-radius': 16,
        'circle-stroke-width': 2,
        'circle-stroke-color': '#ffffff'
      }
    });

    map.addLayer({
      id: 'trip-stop-labels',
      type: 'symbol',
      source: 'trip-stops',
      layout: {
        'text-field': ['case', ['has', 'point_count'],
          ['concat', ['to-string', ['get', 'firstOrder']], '–', ['to-string', ['get', 'lastOrder']]],
          ['to-string', ['get', 'order']]],
        'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
        'text-size': ['case', ['has', 'point_count'], 11, 12],
        'text-allow-overlap': true,
        'text-ignore-placement': true
      },
      paint: { 'text-color': '#ffffff' }
    });

    // A merged circle opens up to where its stops stand apart
    map.on('click', 'trip-stop-groups', (event) => {
      const group = event.features?.[0];
      const groupId = group?.properties?.['cluster_id'];
      if (groupId == null) return;
      (map.getSource('trip-stops') as mapboxgl.GeoJSONSource).getClusterExpansionZoom(groupId, (error, zoom) => {
        if (error || zoom == null) return;
        map.easeTo({ center: (group!.geometry as GeoJSON.Point).coordinates as [number, number], zoom });
      });
    });
    map.on('mouseenter', 'trip-stop-groups', () => { map.getCanvas().style.cursor = 'pointer'; });
    map.on('mouseleave', 'trip-stop-groups', () => { map.getCanvas().style.cursor = ''; });

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
    this.filterPulse?.hide();
    if (this.loadPlacesTimeout) {
      clearTimeout(this.loadPlacesTimeout);
    }
    if (this.mapInstance) {
      this.mapInstance.remove();
    }
  }

  private initMap(): void {
    const lastView = this.mapViewState.get();
    this.mapInstance = new mapboxgl.Map({
      container: 'map',
      style: 'mapbox://styles/mapbox/light-v11',
      projection: 'mercator',
      zoom: lastView?.zoom ?? DEFAULT_ZOOM,
      minZoom: 1.5,
      center: lastView?.center ?? DEFAULT_CENTER,
      bearing: lastView?.bearing ?? 0,
      pitch: lastView?.pitch ?? 0,
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
      this.filterPulse = new FilterPulse(this.mapInstance!);
      this.loadPlacesForViewport();
      // A filter chosen before leaving the map is still on when coming back
      this.updateFilterHighlight();
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
      this.rememberView();
      if (this.tripActive()) return;
      if (this.loadPlacesTimeout) clearTimeout(this.loadPlacesTimeout);
      this.loadPlacesTimeout = setTimeout(() => this.loadPlacesForViewport(), 400);
    });
  }

  private rememberView(): void {
    if (!this.mapInstance) return;
    const center = this.mapInstance.getCenter();
    this.mapViewState.save({
      center: [center.lng, center.lat],
      zoom: this.mapInstance.getZoom(),
      bearing: this.mapInstance.getBearing(),
      pitch: this.mapInstance.getPitch()
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
  }

  // An ongoing event has no date, so it gets a word instead; the API sends UTC without a zone marker
  private formatEventDate(startAt: string | null | undefined): string {
    if (!startAt) return ONGOING_EVENT_LABEL;
    const date = new Date(/[zZ]|[+-]\d\d:\d\d$/.test(startAt) ? startAt : startAt + 'Z');
    return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
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

    const validPlaces = this.lastRenderedPlaces.filter(place =>
      place.latitude >= -90 && place.latitude <= 90 &&
      place.longitude >= -180 && place.longitude <= 180
    );

    this.setDensityData(this.isDensityView ? this.lastDensityCells : []);
    this.setPlacesData(!this.isDensityView ? validPlaces : []);
    this.updatePulses();
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

    registerPawClusterImages(map);
    map.addLayer({
      id: 'density-cells',
      type: 'symbol',
      source: 'density-src',
      layout: pawClusterLayout(['get', 'count'], ['get', 'label']),
      paint: PAW_CLUSTER_PAINT
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
          description: this.truncate(place.description, POPUP_DESCRIPTION_LENGTH) ?? '',
          address: place.address ?? '',
          saveType,
          // A place with something coming up wears a gold ring and the date, like a ticket stub
          eventTitle: place.nextEvent?.title ?? '',
          eventLabel: place.nextEvent ? this.formatEventDate(place.nextEvent.startAt) : '',
          photo: sizedPhotoUrl(place.thumbnailUrl, POPUP_PHOTO_WIDTH_PX) ?? ''
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

    registerPawClusterImages(map);
    // Clusters are bear paws that grow and darken with their count, the number sitting on the main pad
    map.addLayer({
      id: 'place-clusters',
      type: 'symbol',
      source: 'places-src',
      filter: ['has', 'point_count'],
      layout: pawClusterLayout(['get', 'point_count'], ['get', 'point_count_abbreviated']),
      paint: PAW_CLUSTER_PAINT
    });

    map.addLayer({
      id: 'place-unclustered',
      type: 'circle',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      paint: {
        // Festival-style round pins: filled with the category colour, cream ring like the style board
        'circle-color': ['get', 'pinColor'],
        // Individual pins only exist past the paw zooms, and each one is a bear from the start: its dot is just the
        // "ground spot" it stands on. Emoji-only categories get a round badge that holds the emoji instead.
        'circle-radius': [
          'interpolate', ['linear'], ['zoom'],
          DENSITY_MAX_ZOOM, ['case', HAS_BEAR_POSE, 3.5, 8],
          12, ['case', HAS_BEAR_POSE, 5, 12],
          16, ['case', HAS_BEAR_POSE, 6, 15]
        ],
        // Saved state (Want-to-go / Been-there / Loved) moves to a thicker ring so the category colour stays visible
        'circle-stroke-color': ['match', ['get', 'saveType'],
          'WantToGo', SAVE_TYPE_COLORS['WantToGo'],
          'Visited', SAVE_TYPE_COLORS['Visited'],
          'VisitedLoved', SAVE_TYPE_COLORS['VisitedLoved'],
          ['case', HAS_EVENT, EVENT_RING_COLOR, FESTIVAL_PALETTE.cream]],
        'circle-stroke-width': ['case', ['all', ['==', ['get', 'saveType'], 'None'], ['!', HAS_EVENT]], 2, 3.5]
      }
    });

    // As soon as a place stands on its own (no longer inside a paw) it shows its bear, small at first and
    // growing as you zoom in; there is no "bare dot" stage
    map.addLayer({
      id: 'place-icons',
      type: 'symbol',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      layout: {
        // The bear pose when the category has one (and its image has loaded), otherwise the category emoji
        'icon-image': ['coalesce', ['image', ['get', 'bear']], ['image', ['get', 'icon']]],
        // Bears (48 CSS px tall at size 1) stand on the dot; emoji (32 px) sit inside the round badge
        'icon-size': ['interpolate', ['linear'], ['zoom'],
          DENSITY_MAX_ZOOM, 0.5,
          12, ['case', HAS_BEAR_POSE, 0.75, 0.62],
          16, ['case', HAS_BEAR_POSE, 1.1, 0.78]],
        'icon-anchor': ['case', HAS_BEAR_POSE, 'bottom', 'center'],
        'icon-allow-overlap': true,
        'icon-ignore-placement': true,
        // The date hangs under the pin like a ticket stub, once there's room for it
        'text-field': ['step', ['zoom'], '', 12, ['get', 'eventLabel']],
        'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
        'text-size': 11,
        'text-anchor': 'top',
        'text-offset': [0, 0.7],
        'text-allow-overlap': true
      },
      paint: {
        'text-color': FESTIVAL_PALETTE.darkTeal,
        'text-halo-color': EVENT_RING_COLOR,
        'text-halo-width': 2
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
        // A street address (when OSM has one) is more useful than "City, Country"; the city then follows it
        .setHTML(this.buildPlacePopupHtml(
          props['name'],
          props['address'] || props['city'],
          props['address'] ? props['city'] : props['country'],
          props['category'],
          props['description'],
          props['eventTitle'] ? props['eventTitle'] + ' · ' + props['eventLabel'] : null,
          props['photo'] || null))
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
    loadMapImages(map, this.categoryIcons.getAllBearPinImages());
  }

  onCategoryChange(categoryIds: string[]): void {
    this.placesService.selectedCategories.set(categoryIds);
    this.loadPlacesForViewport();
    this.updateFilterHighlight();
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

