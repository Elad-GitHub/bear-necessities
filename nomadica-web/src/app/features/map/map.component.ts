import { Component, AfterViewInit, OnDestroy, ViewChild, ElementRef, signal, NgZone } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
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

type MapMode = 'places' | 'events' | 'both';

const SAVE_TYPE_COLORS: Record<string, string> = {
  WantToGo: '#f9a825',
  Visited: '#5e35b1',
  VisitedLoved: '#e53935'
};
const DEFAULT_PIN_COLOR = '#00796b';

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
  // Spinner only for the very first load — viewport reloads on pan/zoom stay silent (no flicker)
  showInitialSpinner = signal(true);
  private eventMarkers: mapboxgl.Marker[] = [];
  private loadPlacesTimeout: any = null;
  private savedPlaceTypes = new Map<string, string>();
  private lastRenderedPlaces: PlaceSummaryDto[] = [];

  // Trip planner state
  isPlannerOpen = signal(false);
  isPlanning = signal(false);
  tripActive = signal(false);
  tripArea = signal<GeocodeResult | null>(null);
  tripAreaResults = signal<GeocodeResult[]>([]);
  tripStopCount = signal(0);
  private tripCategoryIds: string[] = [];
  private tripMarkers: mapboxgl.Marker[] = [];
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
    this.refreshForMode();
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
      if (params.get('plan') === '1') this.isPlannerOpen.set(true);

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
    this.tripCategoryIds = categoryIds;
  }

  runTripPlan(): void {
    const area = this.tripArea();
    if (!area || !this.mapInstance) return;

    this.isPlanning.set(true);

    // A bare country geocode has no parent country/city in context → search the whole country
    const isCountryResult = !area.city && !area.country;
    const options = isCountryResult
      ? { country: area.name, categoryIds: this.tripCategoryIds }
      : { latitude: area.latitude, longitude: area.longitude, radiusKm: 300, categoryIds: this.tripCategoryIds };

    this.flyToArea(area);

    this.tripsService.getSuggestions(options).subscribe({
      next: (response) => {
        this.renderTrip(response.places || []);
        this.isPlanning.set(false);
        this.isPlannerOpen.set(false);
      },
      error: () => this.isPlanning.set(false)
    });
  }

  clearTrip(): void {
    this.tripActive.set(false);
    this.tripStopCount.set(0);
    this.clearTripMarkers();
    this.removeRouteLayer();
    this.renderPlaces(this.lastRenderedPlaces);
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

  private renderTrip(places: PlaceSummaryDto[]): void {
    if (!this.mapInstance) return;

    this.tripActive.set(true);
    this.setPlacesData([]);
    this.clearEventMarkers();
    this.clearTripMarkers();
    this.removeRouteLayer();

    const valid = places.filter(p =>
      p.latitude >= -90 && p.latitude <= 90 && p.longitude >= -180 && p.longitude <= 180);

    const ordered = this.orderByNearestNeighbor(valid);
    this.tripStopCount.set(ordered.length);

    ordered.forEach((place, index) => {
      const marker = new mapboxgl.Marker({ element: this.buildTripStopElement(place, index + 1) })
        .setLngLat([place.longitude, place.latitude])
        .addTo(this.mapInstance!);
      this.tripMarkers.push(marker);
    });

    if (ordered.length >= 2) {
      this.drawRouteLine(ordered);
      this.fitToStops(ordered);
    } else if (ordered.length === 1) {
      this.mapInstance.flyTo({ center: [ordered[0].longitude, ordered[0].latitude], zoom: 11, duration: 1000 });
    }
  }

  // Greedy nearest-neighbour gives a readable visiting order without a full TSP solve
  private orderByNearestNeighbor(places: PlaceSummaryDto[]): PlaceSummaryDto[] {
    if (places.length <= 2) return [...places];

    const remaining = [...places];
    const area = this.tripArea();
    let current = area
      ? this.takeNearest(remaining, area.longitude, area.latitude)
      : remaining.shift()!;
    const ordered = [current];

    while (remaining.length > 0) {
      current = this.takeNearest(remaining, current.longitude, current.latitude);
      ordered.push(current);
    }
    return ordered;
  }

  private takeNearest(places: PlaceSummaryDto[], lng: number, lat: number): PlaceSummaryDto {
    let nearestIndex = 0;
    let nearestDistance = Number.POSITIVE_INFINITY;
    for (let i = 0; i < places.length; i++) {
      const dLng = places[i].longitude - lng;
      const dLat = places[i].latitude - lat;
      const distance = dLng * dLng + dLat * dLat;
      if (distance < nearestDistance) {
        nearestDistance = distance;
        nearestIndex = i;
      }
    }
    return places.splice(nearestIndex, 1)[0];
  }

  private drawRouteLine(orderedPlaces: PlaceSummaryDto[]): void {
    const straightCoordinates = orderedPlaces.map(p => [p.longitude, p.latitude] as [number, number]);

    // Draw a straight connector immediately, then upgrade to the real road route when it arrives
    this.renderRouteGeometry(straightCoordinates, false);

    this.directionsService.getRoute(straightCoordinates).subscribe(roadCoordinates => {
      if (roadCoordinates && roadCoordinates.length > 1) {
        this.renderRouteGeometry(roadCoordinates, true);
      }
    });
  }

  private renderRouteGeometry(coordinates: [number, number][], isRoad: boolean): void {
    if (!this.mapInstance) return;

    const geojson: any = {
      type: 'Feature',
      geometry: { type: 'LineString', coordinates }
    };

    const existing = this.mapInstance.getSource('trip-route') as mapboxgl.GeoJSONSource | undefined;
    if (existing) {
      existing.setData(geojson);
    } else {
      this.mapInstance.addSource('trip-route', { type: 'geojson', data: geojson });
      this.mapInstance.addLayer({
        id: 'trip-route',
        type: 'line',
        source: 'trip-route',
        layout: { 'line-join': 'round', 'line-cap': 'round' },
        paint: {
          'line-color': '#00796b',
          'line-width': 4,
          'line-opacity': 0.85
        }
      });
    }

    // Straight fallback stays dashed; a real road route is drawn solid
    this.mapInstance.setPaintProperty('trip-route', 'line-dasharray', isRoad ? [1, 0] : [2, 1.5]);
  }

  private fitToStops(orderedPlaces: PlaceSummaryDto[]): void {
    const bounds = new mapboxgl.LngLatBounds();
    orderedPlaces.forEach(p => bounds.extend([p.longitude, p.latitude]));
    this.mapInstance!.fitBounds(bounds, { padding: 80, duration: 1200, maxZoom: 12 });
  }

  private buildTripStopElement(place: PlaceSummaryDto, order: number): HTMLElement {
    const pin = document.createElement('div');
    pin.className = 'trip-stop';
    pin.title = place.name;
    pin.textContent = String(order);
    pin.addEventListener('click', (clickEvent) => {
      clickEvent.stopPropagation();
      this.router.navigate(['/places', place.id]);
    });

    this.attachHoverPopup(pin, place.longitude, place.latitude,
      this.buildPlacePopupHtml(`${order}. ${place.name}`, place.city, place.country,
        this.placeSubcategory(place), place.description));

    return pin;
  }

  private clearTripMarkers(): void {
    this.tripMarkers.forEach(marker => marker.remove());
    this.tripMarkers = [];
  }

  private removeRouteLayer(): void {
    if (!this.mapInstance) return;
    if (this.mapInstance.getLayer('trip-route')) this.mapInstance.removeLayer('trip-route');
    if (this.mapInstance.getSource('trip-route')) this.mapInstance.removeSource('trip-route');
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
        'circle-color': ['step', ['get', 'point_count'], '#4db8a8', 25, '#1e9484', 100, '#00796b'],
        // Small, capped radii so even a 1000-count cluster stays compact and doesn't blanket the map
        'circle-radius': ['step', ['get', 'point_count'], 12, 25, 15, 100, 18],
        'circle-opacity': 0.82,
        'circle-stroke-width': 1.5,
        'circle-stroke-color': '#ffffff'
      }
    });

    map.addLayer({
      id: 'place-cluster-count',
      type: 'symbol',
      source: 'places-src',
      filter: ['has', 'point_count'],
      layout: {
        'text-field': ['get', 'point_count_abbreviated'],
        'text-font': ['DIN Pro Medium', 'Arial Unicode MS Bold'],
        'text-size': 11
      },
      paint: { 'text-color': '#ffffff' }
    });

    map.addLayer({
      id: 'place-unclustered',
      type: 'circle',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      paint: {
        'circle-color': ['match', ['get', 'saveType'],
          'WantToGo', SAVE_TYPE_COLORS['WantToGo'],
          'Visited', SAVE_TYPE_COLORS['Visited'],
          'VisitedLoved', SAVE_TYPE_COLORS['VisitedLoved'],
          DEFAULT_PIN_COLOR],
        // Small at low/medium zoom so pins don't smother each other, growing a touch when zoomed in.
        // Saved pins (Want-to-go / Been-there / Loved) stay slightly larger to stand out — but still compact.
        'circle-radius': [
          'interpolate', ['linear'], ['zoom'],
          9, ['case', ['==', ['get', 'saveType'], 'None'], 3.5, 5],
          16, ['case', ['==', ['get', 'saveType'], 'None'], 6, 7.5]
        ],
        'circle-stroke-width': 1.5,
        'circle-stroke-color': '#ffffff'
      }
    });

    // Category emoji sits over the coloured dot once zoomed in enough to be legible.
    // The dot alone shows at low zoom so pins stay tiny and don't smother each other.
    map.addLayer({
      id: 'place-icons',
      type: 'symbol',
      source: 'places-src',
      filter: ['!', ['has', 'point_count']],
      minzoom: 10,
      layout: {
        'icon-image': ['get', 'icon'],
        'icon-size': ['interpolate', ['linear'], ['zoom'], 10, 0.75, 16, 1.15],
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

