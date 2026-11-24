/** An event on now or within the next hours at a nearby place; startAt is UTC. */
export interface HappeningEventDto {
  id: string;
  title: string;
  placeId: string;
  placeName: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  startAt: string;
  isOngoing: boolean;
}

/** A nearby place open right now; closesAt is its local closing time ("18:00"), null when it never closes. */
export interface OpenPlaceDto {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  closesAt: string | null;
  categoryIds: string[];
  thumbnailUrl?: string | null;
}

export interface HappeningNowResponse {
  events: HappeningEventDto[];
  openPlaces: OpenPlaceDto[];
  placesNearby: number;
  placesWithHours: number;
}
