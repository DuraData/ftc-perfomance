import { readdir, stat } from 'node:fs/promises';
import { resolve } from 'node:path';

const assetsDirectory = resolve(process.cwd(), '../wwwroot/assets');
const maximumJavaScriptBytes = 450 * 1024;
const files = await readdir(assetsDirectory);
const javascriptFiles = files.filter(file => file.endsWith('.js'));

if (javascriptFiles.length < 2) {
  throw new Error('Expected route-level JavaScript chunks, but the production build produced fewer than two files.');
}

const sizes = await Promise.all(javascriptFiles.map(async file => ({
  file,
  bytes: (await stat(resolve(assetsDirectory, file))).size,
})));
sizes.sort((left, right) => right.bytes - left.bytes);

const oversized = sizes.filter(asset => asset.bytes > maximumJavaScriptBytes);
if (oversized.length > 0) {
  const details = oversized.map(asset => `${asset.file}: ${(asset.bytes / 1024).toFixed(1)} KiB`).join(', ');
  throw new Error(`JavaScript bundle budget exceeded (${maximumJavaScriptBytes / 1024} KiB per chunk): ${details}`);
}

console.log(`Bundle budget passed: ${javascriptFiles.length} JavaScript chunks; largest ${sizes[0].file} ${(sizes[0].bytes / 1024).toFixed(1)} KiB.`);
