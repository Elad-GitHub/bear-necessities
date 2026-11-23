import { Injectable } from '@angular/core';
import { GeocodeResult } from '../services/geocoding.service';
import { ItineraryDay } from '../models/trip.model';

export interface ActiveTrip {
  days: ItineraryDay[];
  area: GeocodeResult;
  isAiPlanned: boolean;
  /** Days the traveller asked for; the plan can be shorter when the area runs out of places. */
  requestedDays: number;
}

const STORAGE_KEY = 'active_trip';

/**
 * Keeps the planned trip while the traveller opens a stop's page, so coming back to the map
 * shows the same route instead of an empty map. Kept for the browser tab's lifetime.
 */
@Injectable({ providedIn: 'root' })
export class TripSessionService {
  private activeTrip: ActiveTrip | null = this.readStored();

  get(): ActiveTrip | null {
    return this.activeTrip;
  }

  save(trip: ActiveTrip): void {
    this.activeTrip = trip;
    this.writeStored(trip);
  }

  clear(): void {
    this.activeTrip = null;
    this.writeStored(null);
  }

  private writeStored(trip: ActiveTrip | null): void {
    try {
      if (trip) sessionStorage.setItem(STORAGE_KEY, JSON.stringify(trip));
      else sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage can be blocked (private mode); the in-memory copy still covers in-app navigation
    }
  }

  private readStored(): ActiveTrip | null {
    try {
      const stored = sessionStorage.getItem(STORAGE_KEY);
      return stored ? JSON.parse(stored) as ActiveTrip : null;
    } catch {
      return null;
    }
  }
}
