// Builds the map's paw images from a single-colour paw reference on a light background.
// Usage: node generate-paws.js <reference image> <output dir>
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const [, , referencePath, outputDir] = process.argv;
const CLUSTER_TILT_DEGREES = -9;
const VARIANTS = [
  { name: 'paw-trail', size: 48, fill: [93, 64, 55], fillAlpha: 0.85, tilt: 0 },
  // Clusters are an outlined paw with a faint fill, so the map underneath stays readable
  { name: 'paw-cluster-small', size: 96, fill: [77, 182, 172], fillAlpha: 0.22, stroke: [38, 150, 140], tilt: CLUSTER_TILT_DEGREES },
  { name: 'paw-cluster-medium', size: 96, fill: [38, 166, 154], fillAlpha: 0.26, stroke: [0, 121, 107], tilt: CLUSTER_TILT_DEGREES },
  { name: 'paw-cluster-large', size: 96, fill: [0, 121, 107], fillAlpha: 0.3, stroke: [0, 77, 64], tilt: CLUSTER_TILT_DEGREES }
];
const MARGIN_SHARE = 0.08;
const OUTLINE_SHARE = 0.03;
const OUTLINE_CUT = 40;

const solidWithAlpha = (width, height, [r, g, b], alphaMask) => {
  const pixels = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i++) {
    pixels[i * 4] = r;
    pixels[i * 4 + 1] = g;
    pixels[i * 4 + 2] = b;
    pixels[i * 4 + 3] = alphaMask[i];
  }
  return sharp(pixels, { raw: { width, height, channels: 4 } }).png().toBuffer();
};

(async () => {
  fs.mkdirSync(outputDir, { recursive: true });
  const { data, info } = await sharp(referencePath).removeAlpha().raw().toBuffer({ resolveWithObject: true });

  // Warm brown has far more red than blue; the light-grey background has almost none
  const mask = Buffer.alloc(info.width * info.height);
  for (let i = 0; i < info.width * info.height; i++) {
    const warmth = data[i * 3] - data[i * 3 + 2];
    mask[i] = Math.max(0, Math.min(255, Math.round(((warmth - 25) / 80) * 255)));
  }

  // Square canvas around the paw with a margin, so rotation and the outline never clip
  let minX = info.width, minY = info.height, maxX = 0, maxY = 0;
  for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
    if (mask[y * info.width + x] < 128) continue;
    minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y);
  }
  const side = Math.round(Math.max(maxX - minX, maxY - minY) * (1 + MARGIN_SHARE * 2));
  const squareMask = Buffer.alloc(side * side);
  const offsetX = Math.round((side - (maxX - minX)) / 2) - minX;
  const offsetY = Math.round((side - (maxY - minY)) / 2) - minY;
  for (let y = minY; y <= maxY; y++) for (let x = minX; x <= maxX; x++) {
    squareMask[(y + offsetY) * side + (x + offsetX)] = mask[y * info.width + x];
  }

  // Heel pad = the big blob in the lower part; its centroid is where cluster counts are written
  let sumX = 0, sumY = 0, weight = 0;
  for (let y = Math.round(side * 0.55); y < side; y++) for (let x = 0; x < side; x++) {
    const alpha = squareMask[y * side + x];
    if (alpha < 128) continue;
    sumX += x; sumY += y; weight++;
  }
  const heel = { x: sumX / weight / side - 0.5, y: sumY / weight / side - 0.5 };

  // Grow the paw by about one blur radius: blur, then cut at the level a step edge reaches one sigma out.
  // Each step is its own sharp pass because sharp reorders operations chained on one pipeline.
  const toSingleChannel = (pixels) => sharp(pixels, { raw: { width: side, height: side, channels: 1 } });
  const blurred = await toSingleChannel(squareMask).blur(Math.max(0.5, side * OUTLINE_SHARE)).extractChannel(0).raw().toBuffer();
  const grown = blurred.map(alpha => (alpha >= OUTLINE_CUT ? 255 : 0));
  // Keep a single channel: blur can hand back 3, which would misalign every pixel of the outline
  const outlineMask = await toSingleChannel(grown).blur(1).extractChannel(0).raw().toBuffer();
  if (outlineMask.length !== side * side) throw new Error(`outline mask has ${outlineMask.length / (side * side)} channels`);

  // The stroke is the band the dilated outline adds around each pad, so the fill inside stays see-through
  const strokeMask = outlineMask.map((alpha, i) => Math.max(0, alpha - squareMask[i]));

  for (const variant of VARIANTS) {
    const fillMask = variant.fillAlpha ? squareMask.map(alpha => Math.round(alpha * variant.fillAlpha)) : squareMask;
    const layers = [{ input: await solidWithAlpha(side, side, variant.fill, fillMask) }];
    if (variant.stroke) layers.push({ input: await solidWithAlpha(side, side, variant.stroke, strokeMask) });
    const composed = await sharp({ create: { width: side, height: side, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
      .composite(layers).png().toBuffer();
    const rotated = variant.tilt
      ? await sharp(composed).rotate(variant.tilt, { background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer()
      : composed;
    await sharp(rotated)
      .resize(variant.size, variant.size, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png({ compressionLevel: 9 })
      .toFile(path.join(outputDir, `${variant.name}.png`));
  }

  // Rotation grows the canvas to fit the rotated square, so scale the heel offset by that growth
  const angle = (CLUSTER_TILT_DEGREES * Math.PI) / 180;
  const growth = Math.abs(Math.cos(angle)) + Math.abs(Math.sin(angle));
  const tilted = {
    x: (heel.x * Math.cos(angle) - heel.y * Math.sin(angle)) / growth,
    y: (heel.x * Math.sin(angle) + heel.y * Math.cos(angle)) / growth
  };
  console.log(`heel pad offset (untilted): x=${heel.x.toFixed(4)} y=${heel.y.toFixed(4)}`);
  console.log(`heel pad offset (cluster tilt ${CLUSTER_TILT_DEGREES}°): x=${tilted.x.toFixed(4)} y=${tilted.y.toFixed(4)}`);
})();
