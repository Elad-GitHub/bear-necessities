import { CategoryDto } from './category.model';

export interface EventSummaryDto {
  id: string;
  title: string;
  description?: string;
  placeId?: string;
  latitude?: number;
  longitude?: number;
  startAt?: string;
  endAt?: string;
  eventType?: string;
  isFree?: boolean;
  price?: number;
  currency?: string;
  status?: string;
  categoryIds?: string[];
  thumbnailUrl?: string;
  averageRating?: number;
}

export interface EventOccurrenceDto {
  id: string;
  startAt: string;
  endAt?: string;
}

export interface EventMediaDto {
  id: string;
  url: string;
  type: string;
}

export interface EventDetailDto {
  id: string;
  title: string;
  placeId?: string;
  latitude?: number;
  longitude?: number;
  startAt?: string;
  endAt?: string;
  eventType: string;
  isFree: boolean;
  price?: number;
  currency?: string;
  status: string;
  categoryIds: string[];
  thumbnailUrl?: string;
  averageRating?: number;
  description?: string;
  rRule?: string;
  onlineUrl?: string;
  maxCapacity?: number;
  timezone: string;
  createdAt: string;
  categories: CategoryDto[];
  media: EventMediaDto[];
  upcomingOccurrences: EventOccurrenceDto[];
}

export type RsvpStatus = 'Going' | 'Maybe' | 'NotGoing';

export interface CreateRsvpRequest {
  eventId: string;
  status: RsvpStatus;
  partySize?: number;
  occurrenceId?: string;
}
