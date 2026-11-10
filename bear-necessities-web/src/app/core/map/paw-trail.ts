/** Paw prints the walking mascot leaves along a trip route. */

export const PAW_IMAGE_ID = 'paw-print';
const PAW_IMAGE_SIZE = 48;
const PAW_COLOR = 'rgba(93, 64, 55, 0.85)';
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

/** A paw (main pad + four toes) pointing up, so rotating it by the walking bearing aims it along the route. */
export function drawPawImage(): ImageData | null {
  const canvas = document.createElement('canvas');
  canvas.width = PAW_IMAGE_SIZE;
  canvas.height = PAW_IMAGE_SIZE;
  const context = canvas.getContext('2d');
  if (!context) return null;

  const unit = PAW_IMAGE_SIZE / 48;
  context.fillStyle = PAW_COLOR;
  context.beginPath();
  context.ellipse(24 * unit, 31 * unit, 10 * unit, 9 * unit, 0, 0, Math.PI * 2);
  context.fill();
  for (const [x, y, radiusX, radiusY] of [[11, 19, 4, 5], [19, 12, 4, 5.5], [29, 12, 4, 5.5], [37, 19, 4, 5]]) {
    context.beginPath();
    context.ellipse(x * unit, y * unit, radiusX * unit, radiusY * unit, 0, 0, Math.PI * 2);
    context.fill();
  }
  return context.getImageData(0, 0, PAW_IMAGE_SIZE, PAW_IMAGE_SIZE);
}
