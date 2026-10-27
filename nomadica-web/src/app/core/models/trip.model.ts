import { PlaceSummaryDto } from './place.model';

export const MIN_TRIP_CATEGORIES = 3;
export const MAX_TRIP_DAYS = 14;

export interface ItineraryDay {
  dayNumber: number;
  stops: PlaceSummaryDto[];
  distanceKm: number;
}

export interface Itinerary {
  days: ItineraryDay[];
  totalStops: number;
  planner: string;
}

export interface ItineraryOptions {
  country?: string;
  latitude: number;
  longitude: number;
  categoryIds: string[];
  days: number;
  stopsPerDay?: number;
  radiusKm?: number;
}
