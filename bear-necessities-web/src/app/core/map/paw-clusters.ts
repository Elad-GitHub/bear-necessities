import type mapboxgl from 'mapbox-gl';
import { FESTIVAL_PALETTE } from './festival-map-theme';
import { drawPaw, pawHeelPadOffset } from './paw-trail';

/** Groups of places (Mapbox clusters and server density cells) drawn as bear paws that grow with their count. */

const PAW_CLUSTER_PIXELS = 96;
const PAW_CLUSTER_PIXEL_RATIO = 2;
// Count thresholds where the paw darkens and grows, matching the old cluster circle steps
const MEDIUM_COUNT = 25;
const LARGE_COUNT = 100;
const HUGE_COUNT = 1000;
const ICON_SIZES = { small: 0.85, medium: 1.05, large: 1.25, huge: 1.45 };
const TEXT_SIZE = 12;
// A slight tilt makes the paw read as a print pressed into the ground rather than a flat icon
const PAW_TILT_DEGREES = -9;
const HEEL_PAD_OFFSET = pawHeelPadOffset(PAW_TILT_DEGREES);

const PAW_IMAGES = [
  { id: 'paw-cluster-small', fill: FESTIVAL_PALETTE.turquoise },
  { id: 'paw-cluster-medium', fill: '#26A69A' },
  { id: 'paw-cluster-large', fill: FESTIVAL_PALETTE.deepTeal }
];

export function registerPawClusterImages(map: mapboxgl.Map): void {
  for (const { id, fill } of PAW_IMAGES) {
    if (map.hasImage(id)) continue;
    const paw = drawPaw({
      size: PAW_CLUSTER_PIXELS,
      fill,
      outline: FESTIVAL_PALETTE.cream,
      outlineWidth: 1.6,
      withClaws: true,
      tiltDegrees: PAW_TILT_DEGREES
    });
    if (paw) map.addImage(id, paw, { pixelRatio: PAW_CLUSTER_PIXEL_RATIO });
  }
}

const stepBySize = (count: mapboxgl.ExpressionSpecification, small: unknown, medium: unknown, large: unknown, huge: unknown) =>
  ['step', count, small, MEDIUM_COUNT, medium, LARGE_COUNT, large, HUGE_COUNT, huge] as mapboxgl.ExpressionSpecification;

// The count sits on the heel pad, off the image centre (and shifted by the tilt); offset in ems follows each paw size
const padOffsetEms = (iconSize: number) => {
  const iconPixels = (PAW_CLUSTER_PIXELS / PAW_CLUSTER_PIXEL_RATIO) * iconSize;
  return ['literal', [(HEEL_PAD_OFFSET.x * iconPixels) / TEXT_SIZE, (HEEL_PAD_OFFSET.y * iconPixels) / TEXT_SIZE]];
};

/** Layout for a symbol layer that draws each feature as a paw with its count (or a pre-formatted label) on the pad. */
export function pawClusterLayout(
  count: mapboxgl.ExpressionSpecification,
  label: mapboxgl.ExpressionSpecification
): mapboxgl.SymbolLayerSpecification['layout'] {
  return {
    'icon-image': ['step', count, PAW_IMAGES[0].id, MEDIUM_COUNT, PAW_IMAGES[1].id, LARGE_COUNT, PAW_IMAGES[2].id],
    'icon-size': stepBySize(count, ICON_SIZES.small, ICON_SIZES.medium, ICON_SIZES.large, ICON_SIZES.huge),
    'icon-allow-overlap': true,
    'icon-ignore-placement': true,
    'text-field': label,
    'text-font': ['DIN Pro Bold', 'Arial Unicode MS Bold'],
    'text-size': TEXT_SIZE,
    'text-offset': stepBySize(count,
      padOffsetEms(ICON_SIZES.small), padOffsetEms(ICON_SIZES.medium), padOffsetEms(ICON_SIZES.large), padOffsetEms(ICON_SIZES.huge)),
    'text-allow-overlap': true,
    'text-ignore-placement': true
  };
}

export const PAW_CLUSTER_PAINT: mapboxgl.SymbolLayerSpecification['paint'] = { 'text-color': FESTIVAL_PALETTE.cream };
