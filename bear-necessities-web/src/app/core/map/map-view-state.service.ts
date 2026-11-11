import { Injectable } from '@angular/core';

export interface MapView {
  center: [number, number];
  zoom: number;
  bearing: number;
  pitch: number;
}

const STORAGE_KEY = 'map_view';

/**
 * Remembers where the map was looking, so coming back from a place or event page
 * lands on the same spot instead of the world view. Kept for the browser tab's lifetime.
 */
@Injectable({ providedIn: 'root' })
export class MapViewStateService {
  private lastView: MapView | null = this.readStored();

  get(): MapView | null {
    return this.lastView;
  }

  save(view: MapView): void {
    this.lastView = view;
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(view));
    } catch {
      // Storage can be blocked (private mode); the in-memory copy still covers in-app navigation
    }
  }

  private readStored(): MapView | null {
    try {
      const stored = sessionStorage.getItem(STORAGE_KEY);
      return stored ? JSON.parse(stored) as MapView : null;
    } catch {
      return null;
    }
  }
}
