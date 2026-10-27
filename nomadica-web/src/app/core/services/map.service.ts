import { Injectable, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { environment } from '../../../environments/environment';

declare const mapboxgl: any;

@Injectable({
  providedIn: 'root'
})
export class MapService {
  private map: any = null;
  private markers: Map<string, any> = new Map();

  constructor(@Inject(DOCUMENT) private doc: Document) {
    if (typeof mapboxgl !== 'undefined') {
      mapboxgl.accessToken = environment.mapboxToken;
    }
  }

  initializeMap(container: string, center: [number, number], zoom: number): any {
    if (typeof mapboxgl === 'undefined') {
      throw new Error('Mapbox GL is not loaded');
    }

    this.map = new mapboxgl.Map({
      container,
      style: 'mapbox://styles/mapbox/light-v11',
      center,
      zoom
    });
    return this.map;
  }

  getMap(): any {
    return this.map;
  }

  addMarker(id: string, coordinates: [number, number], popup?: string): any {
    if (!this.map) {
      throw new Error('Map not initialized');
    }

    const marker = new mapboxgl.Marker()
      .setLngLat(coordinates)
      .addTo(this.map);

    if (popup) {
      marker.setPopup(new mapboxgl.Popup().setText(popup));
    }

    this.markers.set(id, marker);
    return marker;
  }

  removeMarker(id: string): void {
    const marker = this.markers.get(id);
    if (marker) {
      marker.remove();
      this.markers.delete(id);
    }
  }

  clearMarkers(): void {
    this.markers.forEach(marker => marker.remove());
    this.markers.clear();
  }

  fitBounds(bounds: any, options?: any): void {
    if (this.map) {
      this.map.fitBounds(bounds, options);
    }
  }

  getCenter(): any {
    return this.map?.getCenter() || null;
  }

  getZoom(): number {
    return this.map?.getZoom() || 0;
  }

  getBounds(): any {
    return this.map?.getBounds() || null;
  }
}
