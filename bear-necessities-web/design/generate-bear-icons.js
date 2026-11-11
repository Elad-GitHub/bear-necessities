// Draws the bear icon set on a 24x24 grid and writes it as an Angular registry.
// Every icon is built from the same few shapes (round head with ears, chunky body, paw prints)
// so they read as one family. Usage (sharp is only needed for the preview sheet):
//   node generate-bear-icons.js <web-root> [preview.png]
const fs = require('fs');
const path = require('path');

const [webRoot, previewPath] = process.argv.slice(2);
const OUTPUT = path.join(webRoot, 'src/app/shared/components/bear-icon/bear-icons.ts');

const round = (value) => Math.round(value * 100) / 100;

// Filled bear head with two round ears
const head = (x, y, r) =>
  `<circle cx="${x}" cy="${y}" r="${r}"/>` +
  `<circle cx="${round(x - 0.8 * r)}" cy="${round(y - 0.75 * r)}" r="${round(0.45 * r)}"/>` +
  `<circle cx="${round(x + 0.8 * r)}" cy="${round(y - 0.75 * r)}" r="${round(0.45 * r)}"/>`;

const body = (x, y, rx, ry, angle = 0) =>
  `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}"${angle ? ` transform="rotate(${angle} ${x} ${y})"` : ''}/>`;

// Arms and legs: thick round-capped strokes through the given points
const limb = (points, width = 2.4) =>
  `<polyline points="${points.map((point) => point.join(',')).join(' ')}" fill="none" stroke="currentColor" ` +
  `stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"/>`;

// Thin outline for props (laptop screen, drum, bookmark...)
const line = (d, width = 1.6, extra = '') =>
  `<path d="${d}" fill="none" stroke="currentColor" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"${extra}/>`;

const fill = (d) => `<path d="${d}"/>`;

// Paw print: one pad and four toes, s is its overall size
const paw = (x, y, s, angle = 0) => {
  const shapes =
    `<ellipse cx="${x}" cy="${round(y + 0.22 * s)}" rx="${round(0.3 * s)}" ry="${round(0.24 * s)}"/>` +
    [[-0.36, -0.08, 0.1], [-0.13, -0.3, 0.11], [0.13, -0.3, 0.11], [0.36, -0.08, 0.1]]
      .map(([dx, dy, r]) => `<circle cx="${round(x + dx * s)}" cy="${round(y + dy * s)}" r="${round(r * s)}"/>`)
      .join('');
  return angle ? `<g transform="rotate(${angle} ${x} ${y})">${shapes}</g>` : shapes;
};

const sparkle = (x, y, r) => line(`M${x} ${y - r}V${y + r}M${x - r} ${y}H${x + r}`, 1.3);

const ICONS = {
  // Navigation
  paw: paw(12, 12.5, 17),
  trail:
    paw(5.4, 20, 6.4, 15) + paw(11.4, 16.4, 6.4, 45) + paw(9.6, 9.2, 6.4, 5) + paw(17.6, 5, 6.4, 40),
  community:
    head(4.4, 11.6, 2.2) + fill('M1 23a3.4 4.6 0 0 1 6.8 0Z') +
    head(19.6, 11.6, 2.2) + fill('M16.2 23a3.4 4.6 0 0 1 6.8 0Z') +
    head(12, 9, 3) + fill('M7.6 23a4.4 6 0 0 1 8.8 0Z'),
  profile: head(12, 9, 4) + fill('M4.5 23a7.5 7 0 0 1 15 0Z'),
  going: line('M5.5 5.5h13a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-13a2 2 0 0 1-2-2v-12a2 2 0 0 1 2-2ZM3.5 10h17M8 3.5v4M16 3.5v4') + paw(12, 15, 6.6),
  saved: line('M6.5 3h11a1 1 0 0 1 1 1v17l-6.5-4.2L5.5 21V4a1 1 0 0 1 1-1Z') + paw(12, 9.6, 6),
  added: line('M12 21.5C7.2 16.2 5 12.6 5 9.5a7 7 0 0 1 14 0c0 3.1-2.2 6.7-7 12Z') + paw(12, 9.4, 5.6),
  approvals: line('M12 2.5l7.5 3V11c0 5-3.2 8.5-7.5 10.5C7.7 19.5 4.5 16 4.5 11V5.5Z') + line('M8.6 12l2.4 2.4 4.6-4.8', 1.9),
  logout: line('M14 20.5H5.5V11a5.5 5.5 0 0 1 8.5-4.6') + line('M10 14h11M18 11l3 3-3 3', 1.8),

  // Categories
  movement:
    head(14.6, 4.8, 2.4) + body(12.2, 11.4, 3, 4.1, 22) +
    limb([[13.6, 9.6], [17.6, 11.4]]) + limb([[10.6, 9.4], [7.2, 7.6]]) +
    limb([[10.6, 14.4], [7.6, 16.8], [5, 16.2]]) + limb([[13, 15], [15, 18.4], [14.4, 21.4]]) +
    line('M2 10h2.6M1.5 13.2h2.6', 1.4),
  music:
    head(10, 6.6, 2.8) + body(10, 14.6, 3.6, 4.4) +
    limb([[7.8, 11.6], [12.6, 8.6]], 2.2) + limb([[12.2, 12], [16, 8.2]], 2.2) +
    line('M12.6 7.9L22 5.6', 1.3) + limb([[8.6, 18.4], [8.2, 21.6]]) + limb([[11.4, 18.4], [11.8, 21.6]]) +
    '<circle cx="3.6" cy="7.4" r="1.1"/>' + line('M4.6 7.4V2.8l2 .8', 1.1),
  spirituality:
    head(12, 5.6, 2.7) + body(12, 12.8, 3.5, 3.8) + fill('M4.5 18.8a7.5 2.9 0 0 1 15 0a7.5 2.9 0 0 1-15 0Z') +
    limb([[9, 11.2], [6.6, 15.8]], 2.2) + limb([[15, 11.2], [17.4, 15.8]], 2.2) +
    sparkle(4.4, 5.4, 1.5) + sparkle(19.6, 5.4, 1.5),
  gatherings:
    fill('M12 18c-2.6-1.8-2-4.6-.2-6.8.3 1.8 2.6 2.4 2.1 5-.2 1.2-1 1.9-1.9 1.8Z') +
    line('M7.6 21.4l8.8-2.6M7.6 18.8l8.8 2.6', 1.6) +
    head(4.2, 9.6, 2.1) + fill('M1 21a3.2 4.6 0 0 1 6.4 0Z') +
    head(19.8, 9.6, 2.1) + fill('M16.6 21a3.2 4.6 0 0 1 6.4 0Z'),
  volunteering:
    head(7.6, 5.8, 2.5) + body(7.6, 13.2, 3.2, 4) + limb([[10, 11], [14, 14.6]]) +
    limb([[6.4, 16.6], [5.8, 21]]) + limb([[8.8, 16.6], [9.4, 21]]) +
    line('M17.5 21V13.6', 1.5) + fill('M17.5 16.2c-2.6-.1-3.6-2.2-3.2-3.6 1.9 0 3.2 1.5 3.2 3.6Z') +
    fill('M17.5 14.4c.1-2.6 2-4 3.9-3.8.1 2-1.4 3.8-3.9 3.8Z') + line('M13.6 21.2h7.6', 1.6),
  coworking:
    head(12, 5.6, 3) + '<circle cx="8.4" cy="10.8" r="1.4"/><circle cx="15.6" cy="10.8" r="1.4"/>' +
    '<rect x="5" y="10.8" width="14" height="7.6" rx="1"/>' + fill('M3.2 19.4h17.6l-1.4 2.4H4.6Z'),
  crafting:
    head(8.6, 6, 2.6) + body(8.6, 13.6, 3.4, 4.2) + limb([[11, 11], [14.6, 8]]) +
    limb([[7.2, 17.2], [6.6, 21.4]]) + limb([[10, 17.2], [10.6, 21.4]]) +
    line('M14.6 8L19.2 3.4', 1.4) + '<circle cx="19.7" cy="2.9" r="1.1"/>' +
    line('M17.6 14.4c2.8 0 4.6 1.4 4.6 3.2s-1.8 3.2-4.6 3.2-4.4-1.2-4.4-2.4 1.8-.8 1.8-2.2-.8-1.8 2.6-1.8Z', 1.3) +
    '<circle cx="17" cy="16.6" r=".8"/><circle cx="19.6" cy="17.2" r=".8"/>',
  healing:
    line('M12 3l3 4-3 8-3-8 3-4ZM9 7h6', 1.4) + line('M5.6 4.6l1.6 1.2M18.4 4.6l-1.6 1.2M5 9.4h1.8M19 9.4h-1.8', 1.3) +
    paw(7.4, 18.4, 7, -25) + paw(16.6, 18.4, 7, 25),
  sexuality:
    head(6.6, 14, 3.1) + fill('M1.6 24a5 5 0 0 1 10 0Z') + head(17.4, 14, 3.1) + fill('M12.4 24a5 5 0 0 1 10 0Z') +
    fill('M12 9.6S8.4 7.4 8.4 5c0-1.4 1.1-2.4 2.3-2.4.7 0 1.3.5 1.3 1 0-.5.6-1 1.3-1 1.2 0 2.3 1 2.3 2.4 0 2.4-3.6 4.6-3.6 4.6Z'),
  wellness:
    head(12, 9.4, 3.3) + fill('M6.4 17a5.6 3.4 0 0 1 11.2 0Z') +
    line('M2 17.4q2.5-1.6 5 0t5 0 5 0 5 0M2 20.8q2.5-1.6 5 0t5 0 5 0 5 0', 1.5) +
    line('M4.4 9.6q-1.2-1.6 0-3.2t0-3.2M19.6 9.6q-1.2-1.6 0-3.2t0-3.2', 1.3),
  nature:
    head(8.6, 5.6, 2.5) + body(8.6, 12.6, 3.2, 4) + limb([[7.6, 16.2], [6.4, 21]]) + limb([[10, 16.2], [11.4, 21]]) +
    limb([[10.6, 10.6], [13.4, 12.2]], 2.2) + line('M13.8 8.2L14.6 21.6', 1.4) +
    fill('M19.6 3.6l3 5.6h-1.6l2.2 4.6h-7.2l2.2-4.6h-1.6Z') + line('M19.6 13.8v3.4', 1.6),
  ceremonies:
    head(7.6, 5.8, 2.6) + body(7.6, 13.4, 3.4, 4.2) + fill('M8.6 3.6c.2-1.8 1.2-3 2.6-3.4-.1 1.6-.9 2.9-2.6 3.4Z') +
    limb([[10, 11.2], [13.6, 12]]) + limb([[6.2, 17], [5.6, 21.2]]) + limb([[9, 17], [9.6, 21.2]]) +
    '<circle cx="17.4" cy="12.6" r="4.2" fill="none" stroke="currentColor" stroke-width="1.7"/>' + line('M17.4 9.8v5.6M14.6 12.6h5.6', 1),
  markets:
    fill('M3.6 4h16.8l1.4 4H2.2Z') + line('M2.2 8a2.4 2 0 0 0 4.9 0 2.4 2 0 0 0 4.9 0 2.4 2 0 0 0 4.9 0 2.4 2 0 0 0 4.9 0', 1.3) +
    line('M3.6 10v11M20.4 10v11', 1.5) + head(12, 13.4, 2.6) + '<rect x="2.4" y="16.8" width="19.2" height="2.6" rx=".6"/>',
  food:
    head(12, 6, 3) + '<circle cx="4.6" cy="12.6" r="1.4"/><circle cx="19.4" cy="12.6" r="1.4"/>' +
    fill('M3.6 12.4h16.8a8.4 7.6 0 0 1-16.8 0Z')
};

// Category slug (parent categories) to icon, so the app can look icons up by slug
const CATEGORY_ICONS = {
  'movement': 'movement', 'events': 'gatherings', 'volunteering': 'volunteering', 'co-working': 'coworking',
  'crafting': 'crafting', 'healing': 'healing', 'music-sound': 'music', 'sexuality': 'sexuality',
  'spirituality': 'spirituality', 'wellness': 'wellness', 'nature': 'nature', 'ceremonies': 'ceremonies',
  'markets': 'markets', 'food': 'food'
};

// Illustrated icons cut from the Canva sheets (see slice-icon-sheet.js) take precedence over the drawn ones
const ICON_IMAGE_DIR = path.join(webRoot, 'public/brand/icons');
const iconImages = fs.existsSync(ICON_IMAGE_DIR)
  ? fs.readdirSync(ICON_IMAGE_DIR).filter((file) => file.endsWith('.png')).map((file) => file.slice(0, -4)).sort()
  : [];

const registry =
  '// Generated by design/generate-bear-icons.js; edit the generator, not this file.\n' +
  '// Inner SVG markup on a 24x24 grid, drawn in currentColor.\n' +
  `export const BEAR_ICONS: Record<string, string> = ${JSON.stringify(ICONS, null, 2)};\n\n` +
  `export type BearIconName = keyof typeof BEAR_ICONS;\n\n` +
  '// Icons with an illustrated PNG in public/brand/icons, shown instead of the drawn SVG\n' +
  `export const BEAR_ICON_IMAGES: ReadonlySet<string> = new Set(${JSON.stringify(iconImages)});\n\n` +
  `export const CATEGORY_BEAR_ICONS: Record<string, string> = ${JSON.stringify(CATEGORY_ICONS, null, 2)};\n`;
fs.mkdirSync(path.dirname(OUTPUT), { recursive: true });
fs.writeFileSync(OUTPUT, registry);
console.log(`Wrote ${Object.keys(ICONS).length} icons to ${OUTPUT}`);

if (previewPath) {
  const sharp = require('sharp');
  const svg = (inner, size, color) =>
    Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="${size}" height="${size}" fill="${color}" color="${color}">${inner}</svg>`);
  const names = Object.keys(ICONS);
  const cell = 120;
  const columns = 8;
  const rows = Math.ceil(names.length / columns);
  (async () => {
    const tiles = [];
    for (const [index, name] of names.entries()) {
      const left = (index % columns) * cell;
      const top = Math.floor(index / columns) * cell;
      tiles.push({ input: await sharp(svg(ICONS[name], 72, '#004d40')).png().toBuffer(), left: left + 8, top: top + 6 });
      tiles.push({ input: await sharp(svg(ICONS[name], 24, '#004d40')).png().toBuffer(), left: left + 88, top: top + 30 });
      tiles.push({
        input: Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${cell}" height="24"><text x="4" y="16" font-family="Arial" font-size="13">${name}</text></svg>`),
        left, top: top + 88
      });
    }
    await sharp({ create: { width: columns * cell, height: rows * cell, channels: 4, background: '#ffffff' } })
      .composite(tiles).png().toFile(previewPath);
    console.log(`Preview: ${previewPath}`);
  })();
}
