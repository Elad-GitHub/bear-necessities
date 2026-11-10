import type mapboxgl from 'mapbox-gl';

/** Colours sampled from the festival-map style board (warm, earthy, hand-drawn feel). */
export const FESTIVAL_PALETTE = {
  deepTeal: '#00796b',
  darkTeal: '#00594f',
  sand: '#F4E1C1',
  cream: '#FFF7E6',
  turquoise: '#4DB6AC',
  sage: '#8CBF6A',
  sunsetOrange: '#F57C00',
  mustard: '#FBC02D',
  softPink: '#F48FB1',
  roadEdge: '#E8D3A7'
} as const;

const GREEN_LANDUSE_CLASSES = ['park', 'grass', 'wood', 'scrub', 'pitch', 'cemetery', 'national_park', 'glacier'];
const PATH_LAYER_PATTERN = /(path|steps|pedestrian)/;
const ROAD_LAYER_PATTERN = /^(tunnel|road|bridge)-(simple|case-simple)$/;
const WATER_LABEL_PATTERN = /^(water|waterway)-.*label$/;
const DARK_LABEL_PATTERN = /(settlement|state|country|continent|natural|road)-.*label$/;
// Our own listings are the only points of interest, like on a festival map
const HIDDEN_LAYERS = ['poi-label'];

/**
 * Recolours the Mapbox light-v11 base layers with the festival palette.
 * Runs once per style load; layers missing from a future style version are skipped, never fatal.
 */
export function applyFestivalTheme(map: mapboxgl.Map): void {
  const palette = FESTIVAL_PALETTE;
  const setPaint = (layerId: string, property: string, value: unknown) => {
    if (map.getLayer(layerId)) map.setPaintProperty(layerId, property as any, value as any);
  };

  setPaint('land', 'background-color', palette.sand);
  setPaint('national-park', 'fill-color', palette.sage);
  setPaint('national-park', 'fill-opacity', 0.35);
  setPaint('landuse', 'fill-color', ['match', ['get', 'class'], GREEN_LANDUSE_CLASSES, palette.sage, '#EFD9B4']);
  setPaint('landuse', 'fill-opacity', 0.45);
  setPaint('land-structure-polygon', 'fill-color', palette.cream);
  setPaint('land-structure-line', 'line-color', palette.cream);
  setPaint('water', 'fill-color', palette.turquoise);
  setPaint('waterway', 'line-color', palette.turquoise);
  setPaint('building', 'fill-color', palette.roadEdge);
  setPaint('building', 'fill-opacity', 0.6);
  setPaint('aeroway-polygon', 'fill-color', '#EAD7B5');
  setPaint('aeroway-line', 'line-color', palette.cream);
  setPaint('admin-0-boundary', 'line-color', '#C9A77C');
  setPaint('admin-1-boundary', 'line-color', '#D9BF95');

  for (const layer of map.getStyle()?.layers ?? []) {
    if (HIDDEN_LAYERS.includes(layer.id)) {
      map.setLayoutProperty(layer.id, 'visibility', 'none');
    } else if (layer.type === 'line' && PATH_LAYER_PATTERN.test(layer.id)) {
      setPaint(layer.id, 'line-color', palette.sunsetOrange);
      setPaint(layer.id, 'line-opacity', 0.55);
      setPaint(layer.id, 'line-dasharray', [1, 2]);
    } else if (layer.type === 'line' && ROAD_LAYER_PATTERN.test(layer.id)) {
      // Cream roads vanish on sand land, so roads take the darker tan edge tone (reads like a dirt track)
      setPaint(layer.id, 'line-color', layer.id.includes('case') ? '#D4B783' : palette.roadEdge);
    } else if (layer.type === 'line' && layer.id.endsWith('-rail')) {
      setPaint(layer.id, 'line-color', '#D9C29A');
    } else if (layer.type === 'symbol' && WATER_LABEL_PATTERN.test(layer.id)) {
      setPaint(layer.id, 'text-color', '#FFFFFF');
      setPaint(layer.id, 'text-halo-color', '#2E9E92');
    } else if (layer.type === 'symbol' && DARK_LABEL_PATTERN.test(layer.id)) {
      setPaint(layer.id, 'text-color', palette.darkTeal);
      setPaint(layer.id, 'text-halo-color', palette.cream);
      setPaint(layer.id, 'text-halo-width', 1.5);
    }
  }
}
