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

interface StoredTrip {
  trip: ActiveTrip;
  savedAt: number;
}

const STORAGE_KEY = 'active_trip';
const KEEP_TRIP_DAYS = 30;
const MS_PER_DAY = 24 * 60 * 60 * 1000;

/**
 * Keeps the planned trip on this device until the traveller exits it, so the route is still there
 * after opening a stop's page, closing the browser, or opening the app without signal.
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
      if (trip) localStorage.setItem(STORAGE_KEY, JSON.stringify({ trip, savedAt: Date.now() } satisfies StoredTrip));
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage can be blocked (private mode); the in-memory copy still covers in-app navigation
    }
  }

  // An old plan from a past trip would be a surprise; it drops after a month
  private readStored(): ActiveTrip | null {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const { trip, savedAt } = JSON.parse(stored) as StoredTrip;
      if (!trip || Date.now() - savedAt > KEEP_TRIP_DAYS * MS_PER_DAY) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return trip;
    } catch {
      return null;
    }
  }
}
