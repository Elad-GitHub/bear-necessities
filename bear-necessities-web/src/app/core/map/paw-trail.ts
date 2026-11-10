import type mapboxgl from 'mapbox-gl';
import { loadMapImages } from './map-images';

/** Paw prints the walking mascot leaves along a trip route. */

export const PAW_IMAGE_ID = 'paw-trail';
// Spacing is set in screen pixels when the walk starts, so the trail reads as steps at any zoom
const SCREEN_PIXELS_PER_PRINT = 26;
const MIN_PRINTS = 6;
const MAX_PRINTS = 120;

/** How many prints fit a route that is this many pixels long on screen. */
export function pawPrintCountFor(routeLengthPixels: number): number {
  return Math.min(MAX_PRINTS, Math.max(MIN_PRINTS, Math.round(routeLengthPixels / SCREEN_PIXELS_PER_PRINT)));
}

export interface PawPrint {
  /** Distance along the path, in the same units as the path's total length */
  distance: number;
  longitude: number;
  latitude: number;
  /** Compass bearing in degrees, 0 = north, clockwise */
  bearing: number;
  /** -1 left foot, 1 right foot */
  side: -1 | 1;
}

/** Degrees with longitude scaled by latitude: plenty for pacing and orienting an animation. */
export function measureSegments(path: [number, number][]): number[] {
  return path.slice(1).map((point, index) => {
    const previous = path[index];
    const longitudeScale = Math.cos((previous[1] * Math.PI) / 180);
    return Math.hypot((point[0] - previous[0]) * longitudeScale, point[1] - previous[1]);
  });
}

/** Evenly spaced, alternating left/right prints along the path. */
export function samplePawPrints(path: [number, number][], segmentLengths: number[], printCount: number): PawPrint[] {
  const totalLength = segmentLengths.reduce((sum, length) => sum + length, 0);
  if (path.length < 2 || totalLength === 0 || printCount < 1) return [];

  const spacing = totalLength / printCount;
  const prints: PawPrint[] = [];

  let segment = 0;
  let segmentStart = 0;
  for (let index = 1; index <= printCount; index++) {
    const distance = Math.min(totalLength, index * spacing);
    while (segment < segmentLengths.length - 1 && distance > segmentStart + segmentLengths[segment]) {
      segmentStart += segmentLengths[segment];
      segment++;
    }
    const from = path[segment];
    const to = path[segment + 1];
    const length = segmentLengths[segment];
    const fraction = length ? (distance - segmentStart) / length : 1;
    const longitudeScale = Math.cos((from[1] * Math.PI) / 180);
    const bearing = (Math.atan2((to[0] - from[0]) * longitudeScale, to[1] - from[1]) * 180) / Math.PI;

    prints.push({
      distance,
      longitude: from[0] + (to[0] - from[0]) * fraction,
      latitude: from[1] + (to[1] - from[1]) * fraction,
      bearing: (bearing + 360) % 360,
      side: index % 2 === 0 ? 1 : -1
    });
  }
  return prints;
}

export function toPawFeatures(prints: PawPrint[]): GeoJSON.Feature[] {
  return prints.map(print => ({
    type: 'Feature',
    geometry: { type: 'Point', coordinates: [print.longitude, print.latitude] },
    properties: { bearing: print.bearing, side: print.side }
  }));
}

/** Trail prints come from design/generate-paws.js (the reference paw in earthy brown). */
export function registerPawTrailImage(map: mapboxgl.Map): void {
  loadMapImages(map, [[PAW_IMAGE_ID, `brand/paws/${PAW_IMAGE_ID}.png`]]);
}
