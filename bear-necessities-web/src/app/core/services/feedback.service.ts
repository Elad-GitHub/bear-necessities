import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { FeedbackDto, SubmitFeedbackRequest } from '../models/feedback.model';

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  constructor(private api: ApiService) {}

  submitFeedback(request: SubmitFeedbackRequest): Observable<{ id: string }> {
    return this.api.post<{ id: string }>('/feedback', request);
  }

  getOpenFeedback(): Observable<{ items: FeedbackDto[] }> {
    return this.api.get<{ items: FeedbackDto[] }>('/feedback/open');
  }

  markFeedbackHandled(feedbackId: string): Observable<boolean> {
    return this.api.post<boolean>(`/feedback/${feedbackId}/handled`, {});
  }
}
