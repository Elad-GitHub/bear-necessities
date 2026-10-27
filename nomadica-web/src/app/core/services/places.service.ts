import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { PlaceSummaryDto, PlaceDetailDto } from '../models/place.model';
import { ApiService } from './api.service';

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

    return this.api.get<any>('/places', params).pipe(
      tap(response => {
        // Extract items array from response { items: [...], nextCursor, totalCount }
        const items = Array.isArray(response) ? response : (response?.items || response?.data || response?.places || []);
        this.places.set(items);
        this.isLoading.set(false);
      })
    );
  }

  getById(id: string): Observable<PlaceDetailDto> {
    return this.api.get<PlaceDetailDto>(`/places/${id}`);
  }
}

