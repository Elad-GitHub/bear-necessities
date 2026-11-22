import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface GeocodeResult {
  name: string;
  fullName: string;
  latitude: number;
  longitude: number;
  address?: string;
  city?: string;
  country?: string;
  countryCode?: string;
  bbox?: [number, number, number, number];
}

const MAPBOX_GEOCODING_URL = 'https://api.mapbox.com/geocoding/v5/mapbox.places';

@Injectable({
  providedIn: 'root'
})
export class GeocodingService {
  constructor(private http: HttpClient) {}

  search(query: string, limit = 6): Observable<GeocodeResult[]> {
    const coordinates = this.tryParseCoordinates(query);
    if (coordinates) {
      return of([{
        name: 'Dropped pin',
        fullName: `${coordinates.latitude}, ${coordinates.longitude}`,
        latitude: coordinates.latitude,
        longitude: coordinates.longitude
      }]);
    }

    const url = `${MAPBOX_GEOCODING_URL}/${encodeURIComponent(query)}.json`;

    return this.http.get<any>(url, {
      params: {
        access_token: environment.mapboxToken,
        limit,
        // Countries & regions included so trip planning by destination works,
        // alongside cities/POIs/addresses for venue picking
        types: 'country,region,place,locality,poi,address'
      }
    }).pipe(
      map(response => (response?.features || []).map((feature: any) => this.toResult(feature))),
      catchError(() => of([]))
    );
  }

  private tryParseCoordinates(query: string): { latitude: number; longitude: number } | null {
    const match = query.trim().match(/^(-?\d{1,2}(?:\.\d+)?)\s*,\s*(-?\d{1,3}(?:\.\d+)?)$/);
    if (!match) return null;

    const latitude = parseFloat(match[1]);
    const longitude = parseFloat(match[2]);
    if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) return null;

    return { latitude, longitude };
  }

  private toResult(feature: any): GeocodeResult {
    const context: any[] = feature.context || [];
    const findEntry = (prefix: string) => context.find(entry => (entry.id || '').startsWith(prefix));
    const findContext = (prefix: string) => findEntry(prefix)?.text;
    // A country result carries its own code; anything smaller finds it in its context
    const isCountry = (feature.id || '').startsWith('country');
    const countryShortCode: string | undefined = isCountry
      ? feature.properties?.short_code
      : findEntry('country')?.short_code;

    return {
      name: feature.text,
      fullName: feature.place_name,
      longitude: feature.center?.[0],
      latitude: feature.center?.[1],
      address: feature.properties?.address || undefined,
      city: findContext('place') || findContext('locality'),
      country: isCountry ? feature.text : findContext('country'),
      countryCode: countryShortCode?.toUpperCase(),
      bbox: Array.isArray(feature.bbox) && feature.bbox.length === 4 ? feature.bbox : undefined
    };
  }
}
