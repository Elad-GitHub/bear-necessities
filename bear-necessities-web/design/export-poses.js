// Exports sliced bear poses at web sizes. Usage: node export-poses.js <pose dir> <output dir>
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const [, , poseDir, outputDir] = process.argv;
// Pins render at ~48 CSS px tall; 96px source keeps them sharp on 2x screens
const PIN_HEIGHT = 96;
const WALK_HEIGHT = 128;
const LARGE_HEIGHT = 320;
const LARGE_POSES = new Set(['waving', 'meditation', 'hiking']);

// Generator leftovers: detached specks below this share of the pose's ink are erased (the floating heart and steam are bigger)
const SPECK_MAX_SHARE = 0.003;

// Erases tiny connected blobs of opaque pixels, then trims, so stray dots don't shift the pose off-centre
const removeSpecks = async (sourcePath) => {
  const { data, info } = await sharp(sourcePath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height } = info;
  const isInk = (index) => data[index * 4 + 3] > 24;
  const label = new Int32Array(width * height).fill(-1);
  const blobs = [];
  for (let start = 0; start < width * height; start++) {
    if (!isInk(start) || label[start] !== -1) continue;
    const pixels = [start];
    label[start] = blobs.length;
    for (let cursor = 0; cursor < pixels.length; cursor++) {
      const index = pixels[cursor];
      const x = index % width;
      const y = (index - x) / width;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx;
        const ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
        const neighbour = ny * width + nx;
        if (isInk(neighbour) && label[neighbour] === -1) {
          label[neighbour] = blobs.length;
          pixels.push(neighbour);
        }
      }
    }
    blobs.push(pixels);
  }
  const totalInk = blobs.reduce((sum, blob) => sum + blob.length, 0);
  let erased = 0;
  for (const blob of blobs) {
    if (blob.length >= totalInk * SPECK_MAX_SHARE) continue;
    for (const index of blob) data[index * 4 + 3] = 0;
    erased += blob.length;
  }
  const cleaned = await sharp(data, { raw: { width, height, channels: 4 } }).png().toBuffer();
  return { image: await sharp(cleaned).trim().png().toBuffer(), erased };
};

(async () => {
  fs.mkdirSync(outputDir, { recursive: true });
  for (const file of fs.readdirSync(poseDir).filter(name => name.endsWith('.png'))) {
    const name = path.basename(file, '.png');
    const height = name.startsWith('walk') ? WALK_HEIGHT : PIN_HEIGHT;
    const { image: source, erased } = await removeSpecks(path.join(poseDir, file));
    if (erased > 0) console.log(`${name}: erased ${erased} stray pixels`);
    await sharp(source).resize({ height }).png({ compressionLevel: 9 }).toFile(path.join(outputDir, `${name}.png`));
    if (LARGE_POSES.has(name)) {
      await sharp(source).resize({ height: LARGE_HEIGHT }).webp({ quality: 88 }).toFile(path.join(outputDir, `${name}-large.webp`));
    }
  }
  const files = fs.readdirSync(outputDir);
  const totalKb = files.reduce((sum, file) => sum + fs.statSync(path.join(outputDir, file)).size, 0) / 1024;
  console.log(`${files.length} files, ${totalKb.toFixed(0)} KB total`);
})();
