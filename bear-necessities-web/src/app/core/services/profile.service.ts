import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface MyRsvpDto {
  id: string;
  eventId: string;
  eventTitle: string;
  eventStartAt?: string;
  placeName?: string;
  status: string;
  partySize: number;
}

export interface MyProfileDto {
  id: string;
  email: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
}

export interface UpdateMyProfileRequest {
  displayName: string;
  bio: string | null;
  avatarMediaId?: string | null;
  removeAvatar?: boolean;
}

export type SaveType = 'WantToGo' | 'Visited' | 'VisitedLoved';

export interface SavedItemDto {
  targetType: string;
  targetId: string;
  saveType: SaveType;
  savedAt: string;
  summary?: {
    name?: string;
    city?: string;
    country?: string;
    imageUrl?: string;
  };
}

export interface PagedResponse<T> {
  items: T[];
  nextCursor?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  constructor(private api: ApiService) {}

  getMyProfile(): Observable<MyProfileDto> {
    return this.api.get<MyProfileDto>('/users/me');
  }

  updateMyProfile(request: UpdateMyProfileRequest): Observable<MyProfileDto> {
    return this.api.put<MyProfileDto>('/users/me', request);
  }

  getMyRsvps(): Observable<PagedResponse<MyRsvpDto>> {
    return this.api.get<PagedResponse<MyRsvpDto>>('/rsvps/mine', { pageSize: 50 });
  }

  cancelRsvp(rsvpId: string): Observable<void> {
    return this.api.delete<void>(`/rsvps/${rsvpId}`);
  }

  getSavedItems(targetType?: string): Observable<PagedResponse<SavedItemDto>> {
    const params: Record<string, any> = { pageSize: 50 };
    if (targetType) params['targetType'] = targetType;
    return this.api.get<PagedResponse<SavedItemDto>>('/saved', params);
  }

  getMyPlaces(): Observable<{ items: any[] }> {
    return this.api.get<{ items: any[] }>('/places/mine');
  }

  getMyEvents(): Observable<{ items: any[] }> {
    return this.api.get<{ items: any[] }>('/events/mine');
  }

  deletePlace(placeId: string): Observable<void> {
    return this.api.delete<void>(`/places/${placeId}`);
  }

  cancelEvent(eventId: string): Observable<void> {
    return this.api.post<void>(`/events/${eventId}/cancel`, {});
  }

  toggleSave(
    targetType: string,
    targetId: string,
    saveType: SaveType = 'WantToGo'
  ): Observable<{ isSaved: boolean; saveType: SaveType | null }> {
    return this.api.post<{ isSaved: boolean; saveType: SaveType | null }>(
      '/saved', { targetType, targetId, saveType });
  }
}
