import type mapboxgl from 'mapbox-gl';

/** Registers images from URLs once; layers that reference them render as soon as each image arrives. */
export function loadMapImages(map: mapboxgl.Map, images: [imageId: string, url: string][], pixelRatio = 2): void {
  for (const [imageId, url] of images) {
    if (map.hasImage(imageId)) continue;
    map.loadImage(url, (error, image) => {
      if (error || !image || map.hasImage(imageId)) return;
      map.addImage(imageId, image, { pixelRatio });
    });
  }
}
