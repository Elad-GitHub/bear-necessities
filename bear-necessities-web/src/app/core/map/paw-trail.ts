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

/** A trail print: small, earthy brown, no outline. */
export function drawPawImage(): ImageData | null {
  return drawPaw({ size: PAW_IMAGE_SIZE, fill: PAW_COLOR });
}

// Bear-track geometry on a 48-unit grid, as [x, y, radiusX, radiusY, rotation radians].
// Deliberately uneven: a wide bean-shaped heel pad and five toes of different sizes on a lopsided arc.
type Ellipse = [number, number, number, number, number];
const HEEL_PAD: Ellipse[] = [[20.5, 33.5, 9.5, 7.2, -0.12], [28, 32, 9, 7.8, 0.18], [24, 30.5, 8, 6, 0]];
const TOES: Ellipse[] = [
  [9.6, 21, 3.4, 4.3, -0.55],
  [15.6, 14.6, 3.9, 4.9, -0.28],
  [23.4, 11.6, 4.2, 5.1, 0.02],
  [31, 13.2, 4, 4.9, 0.3],
  [37.4, 19, 3.3, 4.2, 0.6]
];
// Claw marks sit just beyond each toe, along the toe's own direction
const CLAWS: Ellipse[] = TOES.map(([x, y, , radiusY, rotation]) => {
  const reach = radiusY + 3.2;
  return [x + Math.sin(rotation) * reach, y - Math.cos(rotation) * reach, 1.1, 1.9, rotation];
});
const HEEL_CENTRE = { x: 24.3, y: 32.2 };

/** Where the heel pad's centre lands relative to the image centre, as a share of the image size (for text on the pad). */
export function pawHeelPadOffset(tiltDegrees: number): { x: number; y: number } {
  const angle = (tiltDegrees * Math.PI) / 180;
  const dx = HEEL_CENTRE.x - 24;
  const dy = HEEL_CENTRE.y - 24;
  return {
    x: (dx * Math.cos(angle) - dy * Math.sin(angle)) / 48,
    y: (dx * Math.sin(angle) + dy * Math.cos(angle)) / 48
  };
}

/** A bear track pointing up; an optional outline makes it read on busy map backgrounds. */
export function drawPaw(options: {
  size: number;
  fill: string;
  outline?: string;
  outlineWidth?: number;
  withClaws?: boolean;
  tiltDegrees?: number;
}): ImageData | null {
  const canvas = document.createElement('canvas');
  canvas.width = options.size;
  canvas.height = options.size;
  const context = canvas.getContext('2d');
  if (!context) return null;

  const unit = options.size / 48;
  if (options.tiltDegrees) {
    context.translate(options.size / 2, options.size / 2);
    context.rotate((options.tiltDegrees * Math.PI) / 180);
    context.translate(-options.size / 2, -options.size / 2);
  }

  const shapes = [...HEEL_PAD, ...TOES, ...(options.withClaws ? CLAWS : [])];
  const fillShapes = (grow: number) => {
    for (const [x, y, radiusX, radiusY, rotation] of shapes) {
      context.beginPath();
      context.ellipse(x * unit, y * unit, (radiusX + grow) * unit, (radiusY + grow) * unit, rotation, 0, Math.PI * 2);
      context.fill();
    }
  };

  if (options.outline) {
    context.fillStyle = options.outline;
    fillShapes(options.outlineWidth ?? 1.5);
  }
  context.fillStyle = options.fill;
  fillShapes(0);
  return context.getImageData(0, 0, options.size, options.size);
}
