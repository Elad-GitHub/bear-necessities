// Slices a transparent character sheet into one PNG per pose by finding empty gaps between them.
// Usage: node slice-poses.js <sheet> <output dir> <comma-separated names in reading order>
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const [, , sheetPath, outputDir, namesArg] = process.argv;
const names = namesArg.split(',');
const ALPHA_THRESHOLD = 24;
// Small detached bits (steam, a floating heart) sit closer than this to their pose, so they stay attached
const MERGE_GAP = 40;
const MIN_BAND = 20;

const bands = (occupied, mergeGap) => {
  const result = [];
  let start = -1;
  for (let i = 0; i <= occupied.length; i++) {
    if (i < occupied.length && occupied[i]) {
      if (start < 0) start = i;
    } else if (start >= 0) {
      result.push([start, i - 1]);
      start = -1;
    }
  }
  const merged = [];
  for (const band of result) {
    const last = merged[merged.length - 1];
    if (last && band[0] - last[1] <= mergeGap) last[1] = band[1];
    else merged.push([...band]);
  }
  return merged.filter(([from, to]) => to - from >= MIN_BAND);
};

(async () => {
  fs.mkdirSync(outputDir, { recursive: true });
  const { data, info } = await sharp(sheetPath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const alphaAt = (x, y) => data[(y * info.width + x) * 4 + 3];

  // Columns have clean vertical gaps; rows don't (poses in neighbouring rows overlap in height), so split by column first
  const columnOccupied = Array.from({ length: info.width }, (_, x) => {
    for (let y = 0; y < info.height; y++) if (alphaAt(x, y) > ALPHA_THRESHOLD) return true;
    return false;
  });
  const columns = bands(columnOccupied, MERGE_GAP);

  // Poses can touch vertically, so cut each column at the emptiest row near every expected row boundary
  const rowCountExpected = Math.round(names.length / columns.length);
  const SEARCH_RADIUS = Math.round(info.height / rowCountExpected / 4);
  const byColumn = columns.map(([left, right]) => {
    const inkPerRow = Array.from({ length: info.height }, (_, y) => {
      let ink = 0;
      for (let x = left; x <= right; x++) if (alphaAt(x, y) > ALPHA_THRESHOLD) ink++;
      return ink;
    });
    const cuts = [0];
    for (let boundary = 1; boundary < rowCountExpected; boundary++) {
      const expected = Math.round(boundary * info.height / rowCountExpected);
      let best = expected;
      for (let y = expected - SEARCH_RADIUS; y <= expected + SEARCH_RADIUS; y++)
        if (inkPerRow[y] < inkPerRow[best]) best = y;
      cuts.push(best);
    }
    cuts.push(info.height);
    return cuts.slice(0, -1).map((top, index) => ({ left, top, right, bottom: cuts[index + 1] - 1 }));
  });

  // Back to reading order: row by row, left to right
  const cells = [];
  const rowCount = Math.max(...byColumn.map(column => column.length));
  for (let row = 0; row < rowCount; row++)
    for (const column of byColumn) if (column[row]) cells.push(column[row]);

  console.log(`columns=${columns.length} rows=${byColumn.map(column => column.length).join('/')} cells=${cells.length} names=${names.length}`);
  if (cells.length !== names.length) {
    cells.forEach((cell, index) => console.log(index, JSON.stringify(cell)));
    throw new Error('Cell count does not match names; adjust gaps');
  }

  for (let index = 0; index < cells.length; index++) {
    const { left, top, right, bottom } = cells[index];
    const file = path.join(outputDir, `${names[index]}.png`);
    const cut = await sharp(sheetPath)
      .extract({ left, top, width: right - left + 1, height: bottom - top + 1 })
      .png()
      .toBuffer();
    await sharp(cut).trim().png().toFile(file);
    console.log(`${names[index]}: ${right - left + 1}x${bottom - top + 1}`);
  }
})();
