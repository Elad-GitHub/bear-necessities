import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap, map } from 'rxjs';
import { ApiService } from './api.service';
import { environment } from '../../../environments/environment';

export interface MediaItemDto {
  id: string;
  url: string;
  thumbnailUrl?: string;
  mimeType?: string;
  sortOrder?: number;
  /** Who shared the photo, for the credit under it in the gallery. */
  uploadedByName?: string | null;
  reviewId?: string | null;
}

interface RequestUploadResponse {
  mediaId: string;
  uploadUrl: string;
  objectKey: string;
  cdnUrl: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class MediaService {
  constructor(private api: ApiService, private http: HttpClient) {}

  /** Full presigned flow: request URL → PUT file to storage → confirm. */
  upload(ownerType: 'Place' | 'Event' | 'User', ownerId: string, file: File): Observable<MediaItemDto> {
    return this.api.post<RequestUploadResponse>('/media/request-upload', {
      ownerType,
      ownerId,
      fileName: file.name,
      mimeType: file.type,
      fileSizeBytes: file.size
    }).pipe(
      switchMap(presigned =>
        this.http.put(presigned.uploadUrl, file, {
          headers: { 'Content-Type': file.type }
        }).pipe(
          switchMap(() => this.api.post<any>('/media/confirm-upload', { mediaId: presigned.mediaId })),
          map(() => ({ id: presigned.mediaId, url: this.contentUrl(presigned.mediaId) }))
        )
      )
    );
  }

  /** Image source served through our API rather than the public bucket URL. */
  contentUrl(mediaId: string): string {
    return `${environment.apiUrl}/media/${mediaId}/content`;
  }

  getByOwner(ownerType: 'Place' | 'Event', ownerId: string): Observable<{ items: MediaItemDto[] }> {
    return this.api.get<{ items: MediaItemDto[] }>(`/media/${ownerType}/${ownerId}`).pipe(
      map(response => ({
        items: (response.items || []).map(item => ({ ...item, url: this.contentUrl(item.id) }))
      }))
    );
  }

  delete(mediaId: string): Observable<void> {
    return this.api.delete<void>(`/media/${mediaId}`);
  }
}
