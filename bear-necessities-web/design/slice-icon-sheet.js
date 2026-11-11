// Cuts a generated icon sheet (dark shapes on white, laid out in a grid) into one PNG per icon.
// Each PNG is a black shape whose alpha comes from how dark the sheet pixel is, so the app can
// recolour it with a CSS mask. Usage (needs sharp):
//   node slice-icon-sheet.js <sheet.png> <out-dir> <columns> <rows> name1,name2,...
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const [sheetPath, outDir, columnsArg, rowsArg, namesArg] = process.argv.slice(2);
const columns = Number(columnsArg);
const rows = Number(rowsArg);
const names = namesArg.split(',');
const OUTPUT_SIZE = 128;
const PADDING = 6;
// Pixels lighter than this are background; at or darker than INK they are fully solid
const PAPER = 235;
const INK = 110;

(async () => {
  const { data, info } = await sharp(sheetPath).removeAlpha().greyscale().raw().toBuffer({ resolveWithObject: true });
  const cellWidth = info.width / columns;
  const cellHeight = info.height / rows;
  fs.mkdirSync(outDir, { recursive: true });

  for (const [index, name] of names.entries()) {
    const left = Math.round((index % columns) * cellWidth);
    const top = Math.round(Math.floor(index / columns) * cellHeight);
    const width = Math.round(cellWidth);
    const height = Math.round(cellHeight);

    // Darkness becomes opacity; find the bounding box of the shape while at it
    const alpha = Buffer.alloc(width * height);
    let minX = width, minY = height, maxX = -1, maxY = -1;
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const grey = data[(top + y) * info.width + (left + x)];
        const opacity = Math.max(0, Math.min(1, (PAPER - grey) / (PAPER - INK)));
        const value = Math.round(opacity * 255);
        alpha[y * width + x] = value;
        if (value > 40) {
          minX = Math.min(minX, x); maxX = Math.max(maxX, x);
          minY = Math.min(minY, y); maxY = Math.max(maxY, y);
        }
      }
    }
    if (maxX < 0) throw new Error(`Cell ${index} (${name}) is empty`);

    const shape = await sharp(Buffer.alloc(width * height * 3), { raw: { width, height, channels: 3 } })
      .joinChannel(alpha, { raw: { width, height, channels: 1 } })
      .extract({ left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 })
      .png()
      .toBuffer();

    await sharp(shape)
      .resize(OUTPUT_SIZE - PADDING * 2, OUTPUT_SIZE - PADDING * 2, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .extend({ top: PADDING, bottom: PADDING, left: PADDING, right: PADDING, background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png()
      .toFile(path.join(outDir, `${name}.png`));
  }
  console.log(`Wrote ${names.length} icons to ${outDir}`);
})();
