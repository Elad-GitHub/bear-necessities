import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { PlacesService } from '../../../core/services/places.service';
import { HappeningEventDto, HappeningNowResponse, OpenPlaceDto } from '../../../core/models/happening-now.model';
import { describeDistance, describeEventStart, describeOpenUntil } from './happening-now';

export interface MapPoint {
  latitude: number;
  longitude: number;
}

const LOCATION_TIMEOUT_MS = 6000;
const LOCATION_MAX_AGE_MS = 5 * 60_000;
const NOW_RADIUS_KM = 25;

/** "Happening now near me": what's open and what's on around the visitor, or around the map centre. */
@Component({
  selector: 'app-happening-now-panel',
  templateUrl: './happening-now-panel.component.html',
  styleUrls: ['./happening-now-panel.component.scss'],
  standalone: false
})
export class HappeningNowPanelComponent implements OnInit {
  @Input({ required: true }) mapCenter!: MapPoint;
  @Output() closed = new EventEmitter<void>();
  @Output() placeSelected = new EventEmitter<string>();
  @Output() eventSelected = new EventEmitter<string>();

  isLoading = signal(true);
  isAroundUser = signal(false);
  isOffline = signal(false);
  result = signal<HappeningNowResponse | null>(null);

  readonly describeOpenUntil = describeOpenUntil;
  readonly describeDistance = describeDistance;
  readonly radiusKm = NOW_RADIUS_KM;

  constructor(private placesService: PlacesService) {}

  ngOnInit(): void {
    // Saved copies of "open now" would be wrong by now, so this view needs a connection
    if (!navigator.onLine) {
      this.isLoading.set(false);
      this.isOffline.set(true);
      return;
    }
    if (!navigator.geolocation) {
      this.load(this.mapCenter, false);
      return;
    }
    navigator.geolocation.getCurrentPosition(
      position => this.load({ latitude: position.coords.latitude, longitude: position.coords.longitude }, true),
      () => this.load(this.mapCenter, false),
      { timeout: LOCATION_TIMEOUT_MS, maximumAge: LOCATION_MAX_AGE_MS });
  }

  useMapCenter(): void {
    this.load(this.mapCenter, false);
  }

  describeStart(happening: HappeningEventDto): string {
    return describeEventStart(happening, new Date());
  }

  isEmpty(): boolean {
    const result = this.result();
    return !!result && result.events.length === 0 && result.openPlaces.length === 0;
  }

  trackPlace(_: number, place: OpenPlaceDto): string {
    return place.id;
  }

  private load(center: MapPoint, isAroundUser: boolean): void {
    this.isLoading.set(true);
    this.isAroundUser.set(isAroundUser);
    this.placesService.getHappeningNow(center.latitude, center.longitude, NOW_RADIUS_KM).subscribe({
      next: response => {
        this.result.set(response);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }
}
