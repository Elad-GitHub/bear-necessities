import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { PlaceSummaryDto } from '../models/place.model';
import { EventSummaryDto } from '../models/event.model';

export interface TripSuggestions {
  places: PlaceSummaryDto[];
  events: EventSummaryDto[];
}

@Injectable({
  providedIn: 'root'
})
export class TripsService {
  constructor(private api: ApiService) {}

  getSuggestions(options: {
    country?: string;
    latitude?: number;
    longitude?: number;
    radiusKm?: number;
    categoryIds?: string[];
  }): Observable<TripSuggestions> {
    const params: Record<string, any> = {};
    if (options.country) params['country'] = options.country;
    if (options.latitude != null) params['latitude'] = options.latitude;
    if (options.longitude != null) params['longitude'] = options.longitude;
    if (options.radiusKm) params['radiusKm'] = options.radiusKm;
    if (options.categoryIds?.length) params['categoryIds'] = options.categoryIds;

    return this.api.get<TripSuggestions>('/trips/suggestions', params);
  }
}
