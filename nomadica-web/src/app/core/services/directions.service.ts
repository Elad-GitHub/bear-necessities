import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

const DIRECTIONS_URL = 'https://api.mapbox.com/directions/v5/mapbox';
// Mapbox driving profile accepts at most 25 waypoints per request
const MAX_WAYPOINTS = 25;

@Injectable({
  providedIn: 'root'
})
export class DirectionsService {
  constructor(private http: HttpClient) {}

  /**
   * Returns the road geometry (ordered [lng,lat] pairs) through the given stops,
   * or null when no drivable route exists so the caller can fall back to a straight line.
   */
  getRoute(stops: [number, number][], profile: 'driving' | 'walking' | 'cycling' = 'driving'): Observable<[number, number][] | null> {
    if (stops.length < 2) return of(null);

    const waypoints = stops.slice(0, MAX_WAYPOINTS);
    const coordinates = waypoints.map(([lng, lat]) => `${lng},${lat}`).join(';');
    const url = `${DIRECTIONS_URL}/${profile}/${coordinates}`;

    return this.http.get<any>(url, {
      params: {
        access_token: environment.mapboxToken,
        geometries: 'geojson',
        overview: 'full'
      }
    }).pipe(
      map(response => {
        const geometry = response?.routes?.[0]?.geometry;
        return geometry?.type === 'LineString' ? (geometry.coordinates as [number, number][]) : null;
      }),
      catchError(() => of(null))
    );
  }
}
