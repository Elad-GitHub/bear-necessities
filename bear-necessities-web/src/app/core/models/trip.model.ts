import { PlaceSummaryDto } from './place.model';

export const MIN_TRIP_CATEGORIES = 3;
export const MAX_TRIP_DAYS = 14;
export const MAX_TRIP_WISH_LENGTH = 500;
/** Planner name the API reports when Gemini, not the built-in planner, chose the stops. */
export const AI_PLANNER = 'gemini';

export interface ItineraryDay {
  dayNumber: number;
  stops: PlaceSummaryDto[];
  distanceKm: number;
  title: string | null;
  /** Why each stop was picked, keyed by place id. */
  reasons: Record<string, string>;
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
  wish?: string;
}
