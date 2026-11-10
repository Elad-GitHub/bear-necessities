// Builds HippieLand brand assets (favicon, app icons, navbar logo) from the bear mascot.
// sharp is deliberately not a project dependency: run `npm install sharp@0.33.5` in a scratch folder, then
//   node generate-brand-icons.js bear-mascot-source.webp <output dir> [--preview]
// and copy favicon.ico to public/ and the PNGs to public/brand/ (convert bear-mascot.png to WebP first).
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const [, , sourcePath, outputDir, flag] = process.argv;
const BRAND_TEAL = { r: 0, g: 121, b: 107, alpha: 1 };
const TRANSPARENT = { r: 0, g: 0, b: 0, alpha: 0 };
// Square around hat + face of the 1254px source; the circular badge mask hides the shoulders and backpack
const HEAD_REGION = { left: 225, top: 0, width: 800, height: 800 };

// Round teal badge with the bear's head: reads clearly even at 16px and hides the flat crop edge
const buildBadge = async (headSquare, size) => {
  const circle = Buffer.from(`<svg width="${size}" height="${size}"><circle cx="${size / 2}" cy="${size / 2}" r="${size / 2}" fill="#fff"/></svg>`);
  const head = await sharp(headSquare).resize(size, size).png().toBuffer();
  return sharp({ create: { width: size, height: size, channels: 4, background: BRAND_TEAL } })
    .composite([{ input: head }, { input: circle, blend: 'dest-in' }])
    .png()
    .toBuffer();
};

const squareOnTransparent = (buffer, size, paddingRatio = 0) => {
  const inner = Math.round(size * (1 - paddingRatio * 2));
  return sharp(buffer)
    .resize(inner, inner, { fit: 'contain', background: TRANSPARENT })
    .extend({
      top: Math.floor((size - inner) / 2), bottom: Math.ceil((size - inner) / 2),
      left: Math.floor((size - inner) / 2), right: Math.ceil((size - inner) / 2),
      background: TRANSPARENT
    })
    .png();
};

const onSolidBackground = async (buffer, size, paddingRatio, background) => {
  const foreground = await squareOnTransparent(buffer, size, paddingRatio).toBuffer();
  return sharp({ create: { width: size, height: size, channels: 4, background } })
    .composite([{ input: foreground }])
    .png();
};

// ICO files may embed PNG images directly (Vista+), which keeps every size crisp
const buildIco = (pngBuffers) => {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(pngBuffers.length, 4);
  const entries = [];
  let offset = 6 + 16 * pngBuffers.length;
  for (const { size, data } of pngBuffers) {
    const entry = Buffer.alloc(16);
    entry.writeUInt8(size >= 256 ? 0 : size, 0);
    entry.writeUInt8(size >= 256 ? 0 : size, 1);
    entry.writeUInt8(0, 2);
    entry.writeUInt8(0, 3);
    entry.writeUInt16LE(1, 4);
    entry.writeUInt16LE(32, 6);
    entry.writeUInt32LE(data.length, 8);
    entry.writeUInt32LE(offset, 12);
    entries.push(entry);
    offset += data.length;
  }
  return Buffer.concat([header, ...entries, ...pngBuffers.map(item => item.data)]);
};

(async () => {
  fs.mkdirSync(outputDir, { recursive: true });
  const source = fs.readFileSync(sourcePath);
  const fullBear = await sharp(source).trim().png().toBuffer();
  const headSquare = await sharp(source).extract(HEAD_REGION).png().toBuffer();
  const badge = await buildBadge(headSquare, 1024);

  if (flag === '--preview') {
    const tiles = await Promise.all([
      squareOnTransparent(fullBear, 256).toBuffer(),
      squareOnTransparent(badge, 256).toBuffer(),
      onSolidBackground(badge, 256, 0.1, BRAND_TEAL).then(image => image.toBuffer()),
      // Show the small sizes enlarged with hard pixels, the way a browser tab renders them
      ...[16, 32].map(async size => sharp(await buildBadge(headSquare, size))
        .resize(256, 256, { kernel: 'nearest' }).png().toBuffer())
    ]);
    await sharp({ create: { width: 256 * tiles.length + 10 * (tiles.length - 1), height: 256, channels: 4, background: { r: 245, g: 245, b: 245, alpha: 1 } } })
      .composite(tiles.map((input, index) => ({ input, left: index * 266, top: 0 })))
      .png()
      .toFile(path.join(outputDir, 'preview.png'));
    console.log('preview written');
    return;
  }

  const writes = [
    // Navbar logo: full bear, tall enough for 2x screens
    sharp(fullBear).resize({ height: 160 }).png().toFile(path.join(outputDir, 'bear-logo.png')),
    sharp(fullBear).resize({ height: 1024 }).png().toFile(path.join(outputDir, 'bear-mascot.png')),
    buildBadge(headSquare, 32).then(image => fs.promises.writeFile(path.join(outputDir, 'favicon-32.png'), image)),
    buildBadge(headSquare, 192).then(image => fs.promises.writeFile(path.join(outputDir, 'icon-192.png'), image)),
    buildBadge(headSquare, 512).then(image => fs.promises.writeFile(path.join(outputDir, 'icon-512.png'), image)),
    // Maskable icons get cropped to circles/squircles by Android, so the head sits inside the safe zone
    onSolidBackground(badge, 512, 0.1, BRAND_TEAL).then(image => image.toFile(path.join(outputDir, 'icon-maskable-512.png'))),
    // iOS fills transparency with black, so the home-screen icon needs a solid background
    onSolidBackground(badge, 180, 0.02, BRAND_TEAL).then(image => image.toFile(path.join(outputDir, 'apple-touch-icon.png')))
  ];
  await Promise.all(writes);

  const icoSizes = [16, 32, 48];
  const icoImages = await Promise.all(icoSizes.map(async size => ({ size, data: await buildBadge(headSquare, size) })));
  fs.writeFileSync(path.join(outputDir, 'favicon.ico'), buildIco(icoImages));

  for (const file of fs.readdirSync(outputDir)) {
    console.log(`${file}  ${fs.statSync(path.join(outputDir, file)).size} bytes`);
  }
})();
