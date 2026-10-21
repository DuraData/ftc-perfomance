import fs from 'node:fs';
import path from 'node:path';
import tailwindColors from 'tailwindcss/colors';
import ts from 'typescript';
import tailwindConfig from '../tailwind.config.js';

const sourceRoot = path.resolve('src');
const minimumNormalTextContrast = 4.5;
const failures = [];
const productionStats = { checkedPairs: 0 };

const defaultColorFamilies = [
  'slate', 'gray', 'zinc', 'neutral', 'stone', 'red', 'orange', 'amber', 'yellow', 'lime', 'green',
  'emerald', 'teal', 'cyan', 'sky', 'blue', 'indigo', 'violet', 'purple', 'fuchsia', 'pink', 'rose',
];
const standardColors = Object.fromEntries([
  ['white', tailwindColors.white],
  ['black', tailwindColors.black],
  ...defaultColorFamilies.flatMap(family => Object.entries(tailwindColors[family] ?? {})
    .map(([shade, value]) => [`${family}-${shade}`, value])),
]);
const configuredColors = Object.entries(tailwindConfig.theme?.extend?.colors ?? {})
  .flatMap(([family, shades]) => Object.entries(shades).map(([shade, value]) => [`${family}-${shade}`, value]));
const palette = new Map([...Object.entries(standardColors), ...configuredColors]);

const knownColor = [...palette.keys()].sort((left, right) => right.length - left.length).join('|');
const colorClass = new RegExp(`(?<variants>(?:[a-z-]+:)*)?(?<kind>text|bg)-(?<color>${knownColor})(?![-/\\w])`, 'g');
const opaqueColorCandidate = /(?<variants>(?:[a-z-]+:)*)?(?<kind>text|bg)-(?<color>[a-z]+-\d{2,3}|white|black)(?![-/\w])/g;

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      walk(fullPath);
      continue;
    }
    if ((!entry.name.endsWith('.tsx') && !entry.name.endsWith('.ts'))
      || entry.name.endsWith('.d.ts')
      || entry.name.includes('.test.')
      || entry.name.includes('.spec.'))
      continue;
    inspectSource(fullPath, fs.readFileSync(fullPath, 'utf8'), failures, productionStats);
  }
}

function inspectSource(filePath, sourceText, destination, stats = null) {
  const sourceFile = ts.createSourceFile(filePath, sourceText, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);

  function visit(node) {
    if (ts.isStringLiteralLike(node))
      inspectClasses(filePath, sourceFile, node, node.text, destination, stats);
    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
}

function inspectClasses(filePath, sourceFile, node, value, destination, stats) {
  if (!value.includes('text-') || !value.includes('bg-'))
    return;

  opaqueColorCandidate.lastIndex = 0;
  for (let match = opaqueColorCandidate.exec(value); match; match = opaqueColorCandidate.exec(value)) {
    const color = match.groups?.color;
    if (color && !palette.has(color)) {
      const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
      destination.push(
        `${path.relative(process.cwd(), filePath)}:${position.line + 1} ${match[0]} is not present in the governed Tailwind palette.`,
      );
    }
  }

  const foregrounds = [];
  const backgrounds = [];
  colorClass.lastIndex = 0;
  for (let match = colorClass.exec(value); match; match = colorClass.exec(value)) {
    const token = {
      variants: match.groups?.variants ?? '',
      color: match.groups?.color,
      className: match[0],
    };
    if (match.groups?.kind === 'text') foregrounds.push(token);
    if (match.groups?.kind === 'bg') backgrounds.push(token);
  }

  for (const background of backgrounds) {
    const exactForegrounds = foregrounds.filter(foreground => foreground.variants === background.variants);
    const inheritedForegrounds = exactForegrounds.length ? [] : inheritedVariants(background.variants)
      .flatMap(variants => foregrounds.filter(foreground => foreground.variants === variants))
      .slice(0, 1);
    for (const foreground of exactForegrounds.length ? exactForegrounds : inheritedForegrounds) {
      if (stats)
        stats.checkedPairs += 1;
      const ratio = contrastRatio(palette.get(foreground.color), palette.get(background.color));
      if (ratio + Number.EPSILON >= minimumNormalTextContrast)
        continue;
      const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
      destination.push(
        `${path.relative(process.cwd(), filePath)}:${position.line + 1} ${foreground.className} on ${background.className} is ${ratio.toFixed(2)}:1; require ${minimumNormalTextContrast.toFixed(1)}:1.`,
      );
    }
  }
}

function inheritedVariants(variants) {
  if (!variants)
    return [];
  const parts = variants.split(':').filter(Boolean);
  const environment = parts.filter(part => part === 'dark');
  return environment.length ? [`${environment.join(':')}:`, ''] : [''];
}

function contrastRatio(foreground, background) {
  if (!foreground || !background)
    throw new Error('Contrast governance encountered an unknown colour token.');
  const foregroundLuminance = relativeLuminance(foreground);
  const backgroundLuminance = relativeLuminance(background);
  return (Math.max(foregroundLuminance, backgroundLuminance) + 0.05)
    / (Math.min(foregroundLuminance, backgroundLuminance) + 0.05);
}

function relativeLuminance(hex) {
  const channels = linearRgb(hex);
  return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
}

function linearRgb(color) {
  if (color.startsWith('#')) {
    const expanded = color.length === 4 ? `#${[...color.slice(1)].map(character => character.repeat(2)).join('')}` : color;
    return [1, 3, 5]
      .map(offset => Number.parseInt(expanded.slice(offset, offset + 2), 16) / 255)
      .map(channel => channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4);
  }

  const match = /^oklch\(([\d.]+)%\s+([\d.]+)\s+([\d.]+)\)$/.exec(color);
  if (!match)
    throw new Error(`Unsupported Tailwind colour value: ${color}`);
  const lightness = Number(match[1]) / 100;
  const chroma = Number(match[2]);
  const hue = Number(match[3]) * Math.PI / 180;
  const a = chroma * Math.cos(hue);
  const b = chroma * Math.sin(hue);
  const lRoot = lightness + 0.3963377774 * a + 0.2158037573 * b;
  const mRoot = lightness - 0.1055613458 * a - 0.0638541728 * b;
  const sRoot = lightness - 0.0894841775 * a - 1.2914855480 * b;
  const l = lRoot ** 3;
  const m = mRoot ** 3;
  const s = sRoot ** 3;
  return [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ].map(channel => Math.min(1, Math.max(0, channel)));
}

function selfTest() {
  const detected = [];
  const sourceText = `
    export function Fixture() {
      return <>
        <button className="bg-primary-600 text-white">Safe</button>
        <button className="bg-warning-500 text-white hover:bg-warning-600">Unsafe</button>
        <span className="bg-white text-secondary-400">Unsafe muted text</span>
        <span className="bg-white text-secondary-900 dark:bg-secondary-900 dark:text-secondary-100">Safe themes</span>
        <span className="bg-amber-100 text-amber-800">Safe default palette</span>
        <span className="bg-amber-400 text-white">Unsafe default OKLCH palette</span>
      </>;
    }
  `;
  inspectSource('color-contrast-self-test.tsx', sourceText, detected);
  if (detected.length !== 4
    || !detected.some(failure => failure.includes('bg-warning-500'))
    || !detected.some(failure => failure.includes('hover:bg-warning-600'))
    || !detected.some(failure => failure.includes('text-secondary-400'))
    || !detected.some(failure => failure.includes('bg-amber-400'))) {
    console.error(`Colour contrast self-test failed: expected four representative violations, found ${detected.length}.`);
    process.exit(1);
  }
}

selfTest();
walk(sourceRoot);

if (failures.length > 0) {
  console.error('Explicit production foreground/background pairs must meet WCAG AA normal-text contrast:');
  for (const failure of failures)
    console.error(`- ${failure}`);
  process.exit(1);
}

console.log(`Production colour contrast check passed: ${productionStats.checkedPairs} explicit foreground/background states meet ${minimumNormalTextContrast.toFixed(1)}:1.`);
