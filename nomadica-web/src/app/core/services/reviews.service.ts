import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface ReviewDto {
  id: string;
  authorId: string;
  authorDisplayName: string;
  authorAvatarUrl?: string;
  rating: number;
  body?: string;
  revealedAt?: string;
  targetType: string;
  targetId: string;
}

export interface ReviewsByTargetResponse {
  items: ReviewDto[];
  nextCursor?: string;
  averageRating?: number;
  totalCount: number;
}

export interface CreateReviewRequest {
  targetType: string;
  targetId: string;
  rating: number;
  body?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReviewsService {
  constructor(private api: ApiService) {}

  getByTarget(targetType: string, targetId: string): Observable<ReviewsByTargetResponse> {
    return this.api.get<ReviewsByTargetResponse>(`/reviews/${targetType}/${targetId}`, { pageSize: 20 });
  }

  create(request: CreateReviewRequest): Observable<{ reviewId: string; status: string }> {
    return this.api.post<{ reviewId: string; status: string }>('/reviews', request);
  }
}
