import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';
import tailwindConfig from '../tailwind.config.js';

const sourceRoot = path.resolve('src');
const minimumNormalTextContrast = 4.5;
const failures = [];
const productionStats = { checkedPairs: 0 };

const standardColors = {
  white: '#ffffff',
  black: '#000000',
  'blue-100': '#dbeafe', 'blue-300': '#93c5fd', 'blue-700': '#1d4ed8', 'blue-900': '#1e3a8a',
};
const configuredColors = Object.entries(tailwindConfig.theme?.extend?.colors ?? {})
  .flatMap(([family, shades]) => Object.entries(shades).map(([shade, value]) => [`${family}-${shade}`, value]));
const palette = new Map([...Object.entries(standardColors), ...configuredColors]);

const knownColor = [...palette.keys()].sort((left, right) => right.length - left.length).join('|');
const colorClass = new RegExp(`(?<variants>(?:[a-z-]+:)*)?(?<kind>text|bg)-(?<color>${knownColor})(?![-/\\w])`, 'g');

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      walk(fullPath);
      continue;
    }
    if (!entry.name.endsWith('.tsx') || entry.name.includes('.test.') || entry.name.includes('.spec.'))
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
  const channels = [1, 3, 5]
    .map(offset => Number.parseInt(hex.slice(offset, offset + 2), 16) / 255)
    .map(channel => channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4);
  return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
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
      </>;
    }
  `;
  inspectSource('color-contrast-self-test.tsx', sourceText, detected);
  if (detected.length !== 3
    || !detected.some(failure => failure.includes('bg-warning-500'))
    || !detected.some(failure => failure.includes('hover:bg-warning-600'))
    || !detected.some(failure => failure.includes('text-secondary-400'))) {
    console.error(`Colour contrast self-test failed: expected three representative violations, found ${detected.length}.`);
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
