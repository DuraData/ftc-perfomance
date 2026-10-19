import { readdir, readFile, writeFile } from 'node:fs/promises';
import { relative, resolve } from 'node:path';

const sourceRoot = resolve(process.cwd(), 'src');
const fix = process.argv.includes('--fix');
const buttonPattern = /<button\b[\s\S]*?>/g;

async function findTypeScriptReactFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async entry => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) return findTypeScriptReactFiles(path);
    return entry.isFile() && entry.name.endsWith('.tsx') ? [path] : [];
  }));
  return nested.flat();
}

const findings = [];
for (const file of await findTypeScriptReactFiles(sourceRoot)) {
  const source = await readFile(file, 'utf8');
  const matches = [...source.matchAll(buttonPattern)].filter(match => !/\btype\s*=/.test(match[0]));
  if (matches.length === 0) continue;

  if (fix) {
    await writeFile(file, source.replace(buttonPattern, tag => (
      /\btype\s*=/.test(tag) ? tag : tag.replace('<button', '<button type="button"')
    )));
  }

  for (const match of matches) {
    const line = source.slice(0, match.index).split('\n').length;
    findings.push(`${relative(process.cwd(), file)}:${line}`);
  }
}

if (findings.length > 0 && !fix) {
  throw new Error(`Raw <button> elements must declare an explicit type:\n${findings.join('\n')}`);
}

if (fix) {
  console.log(`Added type="button" to ${findings.length} raw button element(s).`);
} else {
  console.log('Button type governance passed: every raw <button> declares an explicit type.');
}
