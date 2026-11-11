import mapboxgl from 'mapbox-gl';

// Enough to light up a city's worth of matches without flooding the page with elements
const MAX_PULSES = 150;

export interface PulsePoint {
  longitude: number;
  latitude: number;
  /** Paws (zoomed out) get a wider ring than single pins. */
  isLarge: boolean;
}

/**
 * A soft sonar-like ring that keeps rippling out from the places that match the active filter,
 * so a lone result (one retreat on a whole continent) still catches the eye.
 * The rings are small HTML markers animated in CSS: animating the map style itself would keep
 * Mapbox from ever going idle, and the map waits for idle before redrawing places.
 */
export class FilterPulse {
  private markers: mapboxgl.Marker[] = [];

  constructor(private map: mapboxgl.Map) {}

  show(points: PulsePoint[]): void {
    const shown = points.slice(0, MAX_PULSES);
    // Reuse markers so panning around doesn't rebuild every element
    while (this.markers.length > shown.length) this.markers.pop()!.remove();
    shown.forEach((point, index) => {
      const marker = this.markers[index] ?? this.createMarker();
      marker.getElement().classList.toggle('large', point.isLarge);
      // Neighbours ripple slightly out of step, which reads as calmer than everything flashing at once
      marker.getElement().style.animationDelay = `${(index % 6) * 0.25}s`;
      marker.setLngLat([point.longitude, point.latitude]);
      if (!this.markers[index]) {
        marker.addTo(this.map);
        this.markers.push(marker);
      }
    });
  }

  hide(): void {
    this.markers.forEach(marker => marker.remove());
    this.markers = [];
  }

  private createMarker(): mapboxgl.Marker {
    const element = document.createElement('div');
    element.className = 'filter-pulse';
    element.setAttribute('aria-hidden', 'true');
    return new mapboxgl.Marker({ element, anchor: 'center' });
  }
}
