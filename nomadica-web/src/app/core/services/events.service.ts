import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { EventDetailDto, CreateRsvpRequest, EventSummaryDto } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventsService {
  constructor(private api: ApiService) {}

  getById(eventId: string): Observable<any> {
    return this.api.get<any>(`/events/${eventId}`);
  }

  search(query: string, limit = 6): Observable<{ items: EventSummaryDto[] }> {
    return this.api.get<{ items: EventSummaryDto[] }>('/events/search', { query, limit });
  }

  getByPlace(placeId: string): Observable<any> {
    return this.api.get<any>(`/events/by-place/${placeId}`);
  }

  getByViewport(bbox: {
    northEastLat: number; northEastLng: number;
    southWestLat: number; southWestLng: number;
  }): Observable<any> {
    return this.api.get<any>('/events', { ...bbox, pageSize: 50 });
  }

  create(payload: any): Observable<any> {
    return this.api.post<any>('/events', payload);
  }

  createOrUpdateRsvp(request: CreateRsvpRequest): Observable<any> {
    return this.api.post<any>('/rsvps', request);
  }

  cancelRsvp(rsvpId: string): Observable<void> {
    return this.api.delete<void>(`/rsvps/${rsvpId}`);
  }
}
