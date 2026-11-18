import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { PlaceSummaryDto, PlaceDetailDto, PlaceDensityResponse } from '../models/place.model';
import { ApiService } from './api.service';

/** Days chosen in the map's "When?" filter, as YYYY-MM-DD. */
export interface MapDateRange {
  from: string;
  to: string;
}

export interface MapBbox {
  northEastLat: number;
  northEastLng: number;
  southWestLat: number;
  southWestLng: number;
}

@Injectable({
  providedIn: 'root'
})
export class PlacesService {
  places = signal<PlaceSummaryDto[]>([]);
  isLoading = signal(false);
  selectedCategories = signal<string[]>([]);
  // Null means "Anytime": every place, whatever is on
  selectedDates = signal<MapDateRange | null>(null);

  constructor(private api: ApiService) {}

  getByViewport(bbox: MapBbox, categoryIds?: string[], pageSize = 1000): Observable<any> {
    this.isLoading.set(true);

    const params: Record<string, any> = {
      northEastLat: bbox.northEastLat,
      northEastLng: bbox.northEastLng,
      southWestLat: bbox.southWestLat,
      southWestLng: bbox.southWestLng,
      pageSize
    };

    if (categoryIds && categoryIds.length > 0) {
      params['categoryIds'] = categoryIds;
    }
    this.addDates(params);

    return this.api.get<any>('/places', params).pipe(
      tap(response => {
        // Extract items array from response { items: [...], nextCursor, totalCount }
        const items = Array.isArray(response) ? response : (response?.items || response?.data || response?.places || []);
        this.places.set(items);
        this.isLoading.set(false);
      })
    );
  }

  /** Server-side place counts per grid cell; used when zoomed out so every place is counted, not just one page. */
  getDensity(bbox: MapBbox, zoom: number, categoryIds?: string[]): Observable<PlaceDensityResponse> {
    const params: Record<string, any> = { ...bbox, zoom: Math.floor(zoom) };
    if (categoryIds && categoryIds.length > 0) {
      params['categoryIds'] = categoryIds;
    }
    this.addDates(params);
    return this.api.get<PlaceDensityResponse>('/places/density', params);
  }

  private addDates(params: Record<string, any>): void {
    const dates = this.selectedDates();
    if (!dates) return;
    params['from'] = dates.from;
    params['to'] = dates.to;
  }

  getById(id: string): Observable<PlaceDetailDto> {
    return this.api.get<PlaceDetailDto>(`/places/${id}`);
  }

  // Admins take down a wrong automatic photo; the photo search won't pick one again
  removePhoto(id: string): Observable<void> {
    return this.api.delete<void>(`/places/${id}/photo`);
  }
}

