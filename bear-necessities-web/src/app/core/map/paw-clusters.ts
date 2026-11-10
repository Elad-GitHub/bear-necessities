import type mapboxgl from 'mapbox-gl';
import { FESTIVAL_PALETTE } from './festival-map-theme';
import { loadMapImages } from './map-images';

/**
 * Groups of places (Mapbox clusters and server density cells) drawn as bear paws that grow with their count.
 * Images come from design/generate-paws.js (paw-reference.png recoloured, cream outline, tilted).
 */

const PAW_CLUSTER_PIXELS = 96;
const PAW_CLUSTER_PIXEL_RATIO = 2;
// Heel-pad centre relative to the image centre, as a share of the image size (printed by generate-paws.js)
const HEEL_PAD_OFFSET = { x: 0.0343, y: 0.2073 };
// Count thresholds where the paw darkens and grows
const MEDIUM_COUNT = 25;
const LARGE_COUNT = 100;
const HUGE_COUNT = 1000;
const ICON_SIZES = { small: 0.85, medium: 1.05, large: 1.25, huge: 1.45 };
const TEXT_SIZE = 12;

const PAW_IMAGE_IDS = ['paw-cluster-small', 'paw-cluster-medium', 'paw-cluster-large'];

export function registerPawClusterImages(map: mapboxgl.Map): void {
  loadMapImages(map, PAW_IMAGE_IDS.map(id => [id, `brand/paws/${id}.png`]), PAW_CLUSTER_PIXEL_RATIO);
}

const stepBySize = (count: mapboxgl.ExpressionSpecification, small: unknown, medium: unknown, large: unknown, huge: unknown) =>
  ['step', count, small, MEDIUM_COUNT, medium, LARGE_COUNT, large, HUGE_COUNT, huge] as mapboxgl.ExpressionSpecification;

// The count sits on the heel pad, below and slightly right of the image centre; offset in ems follows each paw size
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
    'icon-image': ['step', count, PAW_IMAGE_IDS[0], MEDIUM_COUNT, PAW_IMAGE_IDS[1], LARGE_COUNT, PAW_IMAGE_IDS[2]],
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
