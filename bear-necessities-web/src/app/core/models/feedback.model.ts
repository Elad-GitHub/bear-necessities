export type FeedbackKind = 'bug' | 'idea' | 'other';

export interface SubmitFeedbackRequest {
  kind: FeedbackKind;
  message: string;
  pageUrl: string | null;
  userAgent: string | null;
  contactEmail: string | null;
}

/** A message in the admin inbox; sentBy is empty when the sender wasn't signed in. */
export interface FeedbackDto {
  id: string;
  kind: string;
  message: string;
  pageUrl?: string | null;
  userAgent?: string | null;
  contactEmail?: string | null;
  sentBy?: string | null;
  sentByEmail?: string | null;
  createdAt: string;
}

export const FEEDBACK_MIN_LENGTH = 5;
export const FEEDBACK_MAX_LENGTH = 2000;
