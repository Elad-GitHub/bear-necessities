import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { PlaceSummaryDto } from '../models/place.model';
import { EditSuggestionDto } from '../models/edit-suggestion.model';

export interface PendingPlacesResponse {
  items: PlaceSummaryDto[];
  nextCursor?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  constructor(private api: ApiService) {}

  getPendingPlaces(): Observable<PendingPlacesResponse> {
    return this.api.get<PendingPlacesResponse>('/places/pending', { pageSize: 50 });
  }

  approvePlace(placeId: string): Observable<any> {
    return this.api.post<any>(`/places/${placeId}/approve`, {});
  }

  rejectPlace(placeId: string, reason: string): Observable<any> {
    return this.api.post<any>(`/places/${placeId}/reject`, { reason });
  }

  getPendingEvents(): Observable<{ items: any[] }> {
    return this.api.get<{ items: any[] }>('/events/pending', { pageSize: 50 });
  }

  approveEvent(eventId: string): Observable<any> {
    return this.api.post<any>(`/events/${eventId}/approve`, {});
  }

  rejectEvent(eventId: string, reason: string): Observable<any> {
    return this.api.post<any>(`/events/${eventId}/reject`, { reason });
  }

  getPendingEdits(): Observable<{ items: EditSuggestionDto[] }> {
    return this.api.get<{ items: EditSuggestionDto[] }>('/editsuggestions/pending');
  }

  approveEdit(suggestionId: string): Observable<any> {
    return this.api.post<any>(`/editsuggestions/${suggestionId}/approve`, {});
  }

  rejectEdit(suggestionId: string): Observable<any> {
    return this.api.post<any>(`/editsuggestions/${suggestionId}/reject`, {});
  }

  getPendingMembers(): Observable<{ items: any[] }> {
    return this.api.get<{ items: any[] }>('/users/pending');
  }

  approveMember(userId: string): Observable<any> {
    return this.api.post<any>(`/users/${userId}/approve-membership`, {});
  }

  rejectMember(userId: string): Observable<any> {
    return this.api.post<any>(`/users/${userId}/reject-membership`, {});
  }
}
